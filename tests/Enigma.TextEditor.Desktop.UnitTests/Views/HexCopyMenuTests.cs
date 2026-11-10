using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The hex view's context menu and Ctrl+C in the real window: what they offer, what they put on the clipboard, and
/// which bytes they act on.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HexCopyMenuTests(HeadlessAvaloniaFixture fixture)
{
    // "Hello, hex!" then three bytes that are not printable: 0x00, 0x0A, 0xFF.
    private static readonly byte[] Content = [.. Encoding.ASCII.GetBytes("Hello, hex!"), 0x00, 0x0A, 0xFF, .. Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ")];

    [Fact]
    public void ARightClick_OpensTheCopyCommands()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", Content));
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            ContextMenu menu = RightClick(window, view, 0, HexColumn.Hex, 1);

            Assert.True(menu.IsOpen);
            Assert.Equal(
                ["Copy _Hex String", "Copy _Text", "Copy _Address Range"],
                menu.Items.Select(item => (item as MenuItem)?.Header as string));
            Assert.All(menu.Items.OfType<MenuItem>(), item => Assert.True(item.IsEnabled));
            menu.Close();
            window.Close();
        });

    [Fact]
    public void Ctrl_C_IsShownOnTheItemForTheSelectionsColumn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", Content));
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            ContextMenu menu = RightClick(window, view, 0, HexColumn.Hex, 1);
            Assert.Equal("Ctrl+C", Item(view, "MenuCopyHex").InputGesture?.ToString());
            Assert.Null(Item(view, "MenuCopyText").InputGesture);
            menu.Close();

            // Another byte: the character of byte 1 is the selection's own mirror, and would keep it in hex.
            menu = RightClick(window, view, 0, HexColumn.Text, 5);
            Assert.Null(Item(view, "MenuCopyHex").InputGesture);
            Assert.Equal("Ctrl+C", Item(view, "MenuCopyText").InputGesture?.ToString());
            menu.Close();
            window.Close();
        });

    [Fact]
    public void WithNoSelection_TheMenuOpensFromTheKeyboard_WithItsItemsDisabled()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", Content)))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            Assert.Null(tab.HexSelection);

            view.RowList.RaiseEvent(new ContextRequestedEventArgs());
            Dispatcher.UIThread.RunJobs();
            ContextMenu menu = view.RowList.ContextMenu!;

            Assert.True(menu.IsOpen);
            Assert.All(menu.Items.OfType<MenuItem>(), item => Assert.False(item.IsEnabled));
            menu.Close();
            window.Close();
        });

    [Fact]
    public void EachItem_PutsTheSelectionOnTheClipboard_InItsOwnForm()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", Content)))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            // ", hex!" and the three bytes after it, selected in the hex column.
            tab.HexSelection = new HexSelection(5, 13, HexColumn.Hex);
            Dispatcher.UIThread.RunJobs();

            RightClick(window, view, 0, HexColumn.Hex, 6);
            await ClickAsync(Item(view, "MenuCopyHex"));
            Assert.Equal("2C 20 68 65 78 21 00 0A FF", await ClipboardTextAsync(window));

            RightClick(window, view, 0, HexColumn.Text, 6);
            await ClickAsync(Item(view, "MenuCopyText"));
            Assert.Equal(", hex!...", await ClipboardTextAsync(window));

            RightClick(window, view, 0, HexColumn.Hex, 6);
            await ClickAsync(Item(view, "MenuCopyRange"));
            Assert.Equal("0x00000005 - 0x0000000D", await ClipboardTextAsync(window));

            // Right-clicked inside it every time, the selection is as it was, and the rows have the keyboard back.
            Assert.Equal(new HexSelection(5, 13, HexColumn.Hex), tab.HexSelection);
            Assert.True(view.RowList.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void ARightClickOutsideTheSelection_SelectsTheByteThere_InItsColumn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", Content)))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            tab.HexSelection = new HexSelection(0, 3, HexColumn.Hex);
            Dispatcher.UIThread.RunJobs();

            ContextMenu menu = RightClick(window, view, 1, HexColumn.Text, 2);

            Assert.True(menu.IsOpen);
            Assert.Equal(HexSelection.At(0x12, HexColumn.Text), tab.HexSelection);
            Assert.Equal(1, tab.HexRow);
            await ClickAsync(Item(view, "MenuCopyText"));
            Assert.Equal("E", await ClipboardTextAsync(window));
            window.Close();
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctrl_C_CopiesTheSelection_AsItsColumnShowsIt(bool vim)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = vim);
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", Content)))!;
            MainWindow window = Show(harness);

            tab.HexSelection = new HexSelection(4, 0, HexColumn.Hex);
            await CtrlCAsync(window);
            Assert.Equal("48 65 6C 6C 6F", await ClipboardTextAsync(window));

            tab.HexSelection = new HexSelection(0, 4, HexColumn.Text);
            await CtrlCAsync(window);
            Assert.Equal("Hello", await ClipboardTextAsync(window));
            window.Close();
        });

    [Fact]
    public void ASelectionOverTheLimit_IsNotCopiedAsHexOrText_ButItsRangeIs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("big.bin", new byte[HexDocument.MaxCopyBytes + 16])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            await window.Clipboard!.SetTextAsync("before");

            tab.HexSelection = new HexSelection(0, HexDocument.MaxCopyBytes, HexColumn.Hex);
            RightClick(window, view, 0, HexColumn.Hex, 3);
            await ClickAsync(Item(view, "MenuCopyHex"));

            Assert.Equal("before", await ClipboardTextAsync(window));
            Assert.Equal("The selection is too large to copy as hex: the limit is 1 MB", harness.StatusLine.Current);

            RightClick(window, view, 0, HexColumn.Hex, 3);
            await ClickAsync(Item(view, "MenuCopyRange"));
            Assert.Equal("0x00000000 - 0x00100000", await ClipboardTextAsync(window));

            // Exactly at the limit, it is copied.
            tab.HexSelection = new HexSelection(0, HexDocument.MaxCopyBytes - 1, HexColumn.Text);
            await CtrlCAsync(window);
            Assert.Equal(HexDocument.MaxCopyBytes, (await ClipboardTextAsync(window))?.Length);
            window.Close();
        });

    private static ContextMenu RightClick(MainWindow window, HexView view, int row, HexColumn column, int byteInRow)
    {
        Point point = At(window, view, row, column, byteInRow);

        window.MouseDown(point, MouseButton.Right);
        window.MouseUp(point, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();

        return view.RowList.ContextMenu ?? throw new Xunit.Sdk.XunitException("The rows have no context menu.");
    }

    private static async Task ClickAsync(MenuItem item)
    {
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        item.FindLogicalAncestorOfType<ContextMenu>()?.Close();
        await PumpAsync();
    }

    private static async Task CtrlCAsync(MainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        window.KeyPress(Key.C, RawInputModifiers.Control, PhysicalKey.C, null);
        window.KeyRelease(Key.C, RawInputModifiers.Control, PhysicalKey.C, null);
        await PumpAsync();
    }

    private static async Task<string?> ClipboardTextAsync(MainWindow window) => await window.Clipboard!.TryGetTextAsync();

    private static MenuItem Item(HexView view, string name)
        => view.FindControl<MenuItem>(name) ?? throw new Xunit.Sdk.XunitException($"No menu item {name}.");

    // The middle of a byte's first hex digit, or of its character, in window coordinates.
    private static Point At(MainWindow window, HexView view, int row, HexColumn column, int byteInRow)
    {
        Control container = view.RowList.ContainerFromIndex(row) ?? throw new Xunit.Sdk.XunitException($"Row {row} is not realized.");
        HexRowBlock block = container.GetVisualDescendants().OfType<HexRowBlock>().Single();
        int character = column == HexColumn.Hex
            ? HexFormatter.HexCharIndex(byteInRow, block.OffsetDigits)
            : HexFormatter.TextCharIndex(byteInRow, block.OffsetDigits);
        double left = block.TextLayout.HitTestTextPosition(character).X;
        double right = block.TextLayout.HitTestTextPosition(character + 1).X;

        return block.TranslatePoint(new Point(block.Padding.Left + ((left + right) / 2), block.Bounds.Height / 2), window)!.Value;
    }

    private static async Task PumpAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }

    private static EditorPaneView Pane(MainWindow window) => window.GetVisualDescendants().OfType<EditorPaneView>().First();

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
