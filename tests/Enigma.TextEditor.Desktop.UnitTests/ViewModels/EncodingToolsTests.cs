using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// <em>Reopen with Encoding…</em>, <em>Change File Encoding…</em> and the encoding in the status bar.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class EncodingToolsTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ReopeningAUtf8FileAsWindows1252_DecodesItsBytesThatWay()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Encoding.UTF8.GetBytes("é\n"))))!;
            harness.EncodingPicker.Answers.Enqueue(TextEncodings.Windows1252);

            await harness.Tools.ReopenWithEncodingCommand.ExecuteAsync(null);

            Assert.Equal("Ã©\n", tab.Document.Document.Text);
            Assert.Equal(TextEncodings.Windows1252, tab.Document.Encoding);
            Assert.False(tab.Document.IsDirty);
            Assert.Equal(("Reopen with encoding", TextEncodings.Utf8), Assert.Single(harness.EncodingPicker.Asked));
            Assert.Empty(harness.Confirm.Asked);
        });

    [Fact]
    public void ReopeningADirtyBuffer_AsksFirst_AndNoKeepsTheChanges()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!;
            tab.Document.Document.Insert(0, "edited ");
            harness.EncodingPicker.Answers.Enqueue(TextEncodings.Windows1252);
            harness.Confirm.Answers.Enqueue(false);

            await harness.Tools.ReopenWithEncodingCommand.ExecuteAsync(null);

            Assert.Equal(["Discard unsaved changes?"], harness.Confirm.Asked);
            Assert.Equal("edited abc", tab.Document.Document.Text);
            Assert.Equal(TextEncodings.Utf8, tab.Document.Encoding);
        });

    [Fact]
    public void ReopeningADirtyBuffer_WhenConfirmed_ReplacesTheText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!;
            tab.Document.Document.Insert(0, "edited ");
            harness.EncodingPicker.Answers.Enqueue(TextEncodings.Utf8);
            harness.Confirm.Answers.Enqueue(true);

            await harness.Tools.ReopenWithEncodingCommand.ExecuteAsync(null);

            Assert.Equal("abc", tab.Document.Document.Text);
            Assert.False(tab.Document.IsDirty);
        });

    [Fact]
    public void AnUntitledBuffer_CannotBeReopened()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            harness.Workspace.NewDocument();

            Assert.False(harness.Tools.ReopenWithEncodingCommand.CanExecute(null));
            Assert.True(harness.Tools.ChangeEncodingCommand.CanExecute(null));
        });

    [Fact]
    public void ChangingToUtf16_MarksTheBufferUnsaved_AndTheSaveWritesUtf16()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "hi");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            harness.EncodingPicker.Answers.Enqueue(TextEncodings.Utf16Le);

            await harness.Tools.ChangeEncodingCommand.ExecuteAsync(null);

            Assert.True(tab.Document.IsDirty);
            Assert.Equal(TextEncodings.Utf16Le, tab.Document.Encoding);

            await harness.Workspace.SaveAsync(tab.Document);

            Assert.False(tab.Document.IsDirty);
            Assert.Equal(new byte[] { 0xFF, 0xFE, 0x68, 0x00, 0x69, 0x00 }, File.ReadAllBytes(path));
        });

    [Fact]
    public void AChangedEncoding_StaysUnsavedThroughAnUndo()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "hi")))!;
            tab.Document.ChangeEncoding(TextEncodings.Utf8WithBom);
            tab.Document.Document.Insert(0, "x");

            tab.Document.Document.UndoStack.Undo();

            Assert.True(tab.Document.IsDirty);
        });

    [Fact]
    public void ChangingToTheSameEncoding_ChangesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "hi")))!;

            tab.Document.ChangeEncoding(TextEncodings.Utf8);

            Assert.False(tab.Document.IsDirty);
        });

    [Fact]
    public void AnEncodingThatCannotHoldTheText_RefusesTheSave_AndTheBufferStaysUnsaved()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "日本");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            harness.EncodingPicker.Answers.Enqueue(TextEncodings.Windows1252);
            await harness.Tools.ChangeEncodingCommand.ExecuteAsync(null);

            bool saved = await harness.Workspace.SaveAsync(tab.Document);

            Assert.False(saved);
            Assert.True(tab.Document.IsDirty);
            Assert.Equal("日本", File.ReadAllText(path));
            (string title, string message) = Assert.Single(harness.Messages.Errors);
            Assert.Equal("This file could not be saved", title);
            Assert.Contains("'日' on line 1, column 1 cannot be saved in Windows-1252", message, System.StringComparison.Ordinal);
        });

    [Fact]
    public void TheStatusBar_ShowsTheActiveBuffersEncoding()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "hi")))!;
            MainWindow window = new() { DataContext = harness.Window };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Button status = window.FindControl<Button>("EncodingStatus")!;

            Assert.Equal("UTF-8", status.Content);

            tab.Document.ChangeEncoding(TextEncodings.Utf16Be);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("UTF-16 BE", status.Content);

            await harness.Workspace.OpenAsync(harness.Folder.Write("legacy.txt", new byte[] { 0xE9 }));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Windows-1252", status.Content);

            MenuFlyout flyout = Assert.IsType<MenuFlyout>(status.Flyout);
            Assert.Equal(2, flyout.Items.OfType<MenuItem>().Count());
            window.Close();
        });
}
