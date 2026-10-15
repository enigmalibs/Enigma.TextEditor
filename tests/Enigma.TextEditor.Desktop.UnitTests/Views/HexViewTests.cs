using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The read-only hex viewer: opening, what it allows, how it scrolls, and the Vim keys it keeps.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HexViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void OpeningAsHex_ShowsAReadOnlyHexTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("data.bin", new byte[100]);

            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(path))!;

            Assert.Equal("data.bin (hex)", tab.Document.Title);
            Assert.True(tab.Document.IsHex);
            Assert.Equal(7, tab.Document.Hex!.Rows.Count);
            Assert.False(harness.Workspace.SaveCommand.CanExecute(null));
            Assert.False(harness.Workspace.SaveAsCommand.CanExecute(null));
            Assert.False(harness.Tools.ChangeEncodingCommand.CanExecute(null));
            Assert.False(harness.Tools.SetLanguageCommand.CanExecute(null));
            Assert.False(harness.Tools.UseCrLfCommand.CanExecute(null));
            Assert.Equal("HEX", harness.StatusBar.ModeText);
            Assert.Equal("100 bytes", harness.StatusBar.SizeText);
            Assert.True(harness.StatusBar.IsHexTab);
            Assert.False(harness.StatusBar.IsTextTab);
        });

    [Fact]
    public void AFileCanBeOpenAsTextAndAsHexAtOnce()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("both.txt", "abc");

            EditorTabViewModel text = (await harness.Workspace.OpenAsync(path))!;
            EditorTabViewModel hex = (await harness.Workspace.OpenHexAsync(path))!;
            EditorTabViewModel again = (await harness.Workspace.OpenHexAsync(path))!;

            Assert.NotSame(text.Document, hex.Document);
            Assert.Same(hex, again);
            Assert.Equal(2, harness.Registry.Documents.Count);
        });

    [Fact]
    public void ColonW_IsRefused_AndColonQClosesTheTabAndLetsTheFileGo()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("data.bin", new byte[] { 1, 2, 3 });
            await harness.Workspace.OpenAsync(harness.Folder.Write("keep.txt", "x"));
            await harness.Workspace.OpenHexAsync(path);

            await harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(VimExCommand.Write));
            Assert.Equal("The hex view is read-only", harness.StatusLine.Current);

            await harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(VimExCommand.WriteQuit));
            Assert.Equal(["keep.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));

            // The hex buffer held the file open; closing its last tab released it.
            File.Delete(path);
            Assert.False(File.Exists(path));
        });

    [Fact]
    public void AHexTab_SplitsLikeAnyOther()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[40])))!;

            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);

            Assert.Same(tab.Document, second.ActiveTab?.Document);
            Assert.True(second.IsHexActive);
        });

    [Fact]
    public void AMegabyteFile_RealizesOnlyAScreenful_AndGReachesTheLastRow()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("big.bin", new byte[1024 * 1024]);
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(path))!;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            ListBox rows = pane.HexView.RowList;

            Assert.True(rows.IsEffectivelyVisible);
            Assert.False(pane.EditorControl.IsEffectivelyVisible);
            Assert.Equal(65536, rows.ItemCount);
            Assert.True(rows.GetRealizedContainers().Count() < 200, $"{rows.GetRealizedContainers().Count()} rows realized");
            Assert.True(tab.Document.Hex!.Rows.PagesRead < 10, $"{tab.Document.Hex.Rows.PagesRead} pages read");

            window.KeyTextInput("G");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(65535, tab.HexRow);
            Assert.Equal("Offset 0x000FFFF0", window.FindControl<TextBlock>("CaretStatus")?.Text);

            window.KeyTextInput("gg");
            window.KeyTextInput("jj");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, tab.HexRow);

            window.KeyTextInput("k");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, tab.HexRow);
            window.Close();
        });

    [Fact]
    public void ColonQTypedInTheHexView_ClosesTheTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("keep.txt", "x"));
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[32]));
            MainWindow window = Show(harness);

            window.KeyTextInput(":q");
            Assert.Equal(":q", window.FindControl<TextBlock>("CommandLine")?.Text);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            for (int round = 0; round < 5; round++)
            {
                Dispatcher.UIThread.RunJobs();
                await System.Threading.Tasks.Task.Delay(10);
            }

            Assert.Equal(["keep.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            window.Close();
        });

    [Fact]
    public void TheEditMenu_DoesNothingOnAHexTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[32]));
            MainWindow window = Show(harness);

            Assert.Null(window.ActiveEditor);
            window.Close();
        });

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().First().FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
