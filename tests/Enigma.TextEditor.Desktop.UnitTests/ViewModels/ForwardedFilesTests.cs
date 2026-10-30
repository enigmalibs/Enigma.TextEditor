using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// Files another launch handed over to the running instance: where they open, the new tab they replace, and the
/// window coming forward for them.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ForwardedFilesTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void HandedOverFiles_OpenInTheActivePane_OnTheFilesPage()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            harness.Workspace.Split(SplitAxis.Columns, newDocument: false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));
            EditorPaneViewModel active = harness.Workspace.ActivePane;
            harness.Shell.ShowSettings();

            await harness.Window.OpenForwardedAsync(Read(harness, "c.txt", "d.txt"));

            Assert.True(harness.Shell.IsFilesPage);
            Assert.Same(active, harness.Workspace.ActivePane);
            Assert.Equal(["b.txt", "c.txt", "d.txt"], active.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("d.txt", active.ActiveTab?.Document.Title);
        });

    [Fact]
    public void ANewTabShowing_GivesItsPlaceToTheFirstFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenStartupFilesAsync(new StartupFiles([], []));
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            Assert.True(Assert.Single(pane.Tabs).Document.IsUntitled);

            await harness.Window.OpenForwardedAsync(Read(harness, "c.txt", "d.txt"));

            Assert.Equal(["c.txt", "d.txt"], pane.Tabs.Select(tab => tab.Document.Title));
        });

    [Fact]
    public void AnUntitledTabWithText_IsKept()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel untitled = harness.Workspace.NewDocument();
            untitled.Document.Document.Insert(0, "draft");

            await harness.Window.OpenForwardedAsync(Read(harness, "c.txt"));

            Assert.Equal([untitled.Document.Title, "c.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
        });

    [Fact]
    public void AMissingFile_IsANewBuffer_AndAFolderIsReported()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            Directory.CreateDirectory(harness.Folder.PathOf("folder"));
            string missing = harness.Folder.PathOf("new.txt");

            await harness.Window.OpenForwardedAsync(
                StartupFiles.Parse([missing, harness.Folder.PathOf("folder")], harness.Folder.Root, File.Exists, Directory.Exists));

            Assert.Equal(missing, harness.Workspace.ActiveDocument?.FilePath);
            Assert.False(File.Exists(missing));
            Assert.Contains("folder", harness.StatusLine.Current, System.StringComparison.Ordinal);
        });

    [Fact]
    public void BringingTheWindowForward_RestoresItAsItWasBeforeItWasMinimised()
        => fixture.RunAsync(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.WindowState = WindowState.Maximized;
            window.WindowState = WindowState.Minimized;
            window.BringToFront();
            Assert.Equal(WindowState.Maximized, window.WindowState);

            window.WindowState = WindowState.Normal;
            window.WindowState = WindowState.Minimized;
            window.BringToFront();
            Assert.Equal(WindowState.Normal, window.WindowState);

            window.BringToFront();
            Assert.Equal(WindowState.Normal, window.WindowState);
            window.Close();

            return Task.CompletedTask;
        });

    // As the receiving instance reads them: full paths, written first so they exist.
    private static StartupFiles Read(EditorHarness harness, params string[] names)
        => StartupFiles.Parse(
            [.. names.Select(name => harness.Folder.Write(name, name))],
            harness.Folder.Root,
            File.Exists,
            Directory.Exists);
}
