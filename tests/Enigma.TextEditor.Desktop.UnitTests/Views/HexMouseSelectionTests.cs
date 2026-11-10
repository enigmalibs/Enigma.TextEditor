using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Selecting bytes in the hex view with the mouse, in the real window: a press, a drag that keeps to its column,
/// Shift with a press, and the rows' edges.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HexMouseSelectionTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void APress_SelectsTheByteUnderThePointer_InItsColumn_AndMakesItsRowCurrent()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            Click(window, At(window, view, 2, HexColumn.Hex, 5));

            Assert.Equal(HexSelection.At(0x25, HexColumn.Hex), tab.HexSelection);
            Assert.Equal(2, tab.HexRow);
            Assert.True(view.RowList.IsKeyboardFocusWithin);

            Click(window, At(window, view, 3, HexColumn.Text, 9));

            Assert.Equal(HexSelection.At(0x39, HexColumn.Text), tab.HexSelection);
            Assert.Equal(3, tab.HexRow);
            window.Close();
        });

    [Fact]
    public void APressOnEitherDigitOrTheSpaceAfter_SelectsThatPair()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[64])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            HexRowBlock row = Row(view, 1);
            int pair = HexFormatter.HexCharIndex(7, 8);

            for (int character = pair; character < pair + 3; character++)
            {
                Click(window, CharacterCentre(window, row, character));
                Assert.Equal(HexSelection.At(0x17, HexColumn.Hex), tab.HexSelection);
            }

            window.Close();
        });

    [Fact]
    public void ADragFromTheHexColumn_SelectsOnlyHex_EvenOverTheText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            window.MouseDown(At(window, view, 1, HexColumn.Hex, 3), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            window.MouseMove(At(window, view, 3, HexColumn.Text, 10), RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();

            // Over the text, the drag stays in the hex column: at the row's last pair.
            Assert.Equal(new HexSelection(0x13, 0x3F, HexColumn.Hex), tab.HexSelection);
            Assert.Equal(3, tab.HexRow);

            window.MouseMove(At(window, view, 0, HexColumn.Hex, 0), RawInputModifiers.LeftMouseButton);
            window.MouseUp(At(window, view, 0, HexColumn.Hex, 0), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new HexSelection(0x13, 0x00, HexColumn.Hex), tab.HexSelection);
            Assert.Equal(0, tab.HexRow);

            // Released: moving on changes nothing.
            window.MouseMove(At(window, view, 4, HexColumn.Hex, 4));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new HexSelection(0x13, 0x00, HexColumn.Hex), tab.HexSelection);
            window.Close();
        });

    [Fact]
    public void ADragFromTheTextColumn_SelectsOnlyText_EvenOverTheHex()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            window.MouseDown(At(window, view, 1, HexColumn.Text, 3), MouseButton.Left);
            window.MouseMove(At(window, view, 2, HexColumn.Text, 6), RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new HexSelection(0x13, 0x26, HexColumn.Text), tab.HexSelection);

            // Over the hex, the drag stays in the text column: at the row's first character.
            window.MouseMove(At(window, view, 2, HexColumn.Hex, 9), RawInputModifiers.LeftMouseButton);
            window.MouseUp(At(window, view, 2, HexColumn.Hex, 9), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new HexSelection(0x13, 0x20, HexColumn.Text), tab.HexSelection);
            window.Close();
        });

    [Fact]
    public void ShiftWithAPress_ExtendsFromWhereTheSelectionStarted_InItsColumn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            Click(window, At(window, view, 0, HexColumn.Hex, 2));
            Click(window, At(window, view, 2, HexColumn.Hex, 4), RawInputModifiers.Shift);
            Assert.Equal(new HexSelection(2, 0x24, HexColumn.Hex), tab.HexSelection);

            Click(window, At(window, view, 0, HexColumn.Hex, 0), RawInputModifiers.Shift);
            Assert.Equal(new HexSelection(2, 0, HexColumn.Hex), tab.HexSelection);
            Assert.Equal(0, tab.HexRow);
            window.Close();
        });

    [Fact]
    public void APressPastTheLastByteOfAShortRow_SelectsItsLastByte()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            double lastRow = At(window, view, 6, HexColumn.Hex, 0).Y;

            // The last row holds 0x60 to 0x63: the place of a twelfth pair, or of a twelfth character, is past it.
            Click(window, At(window, view, 0, HexColumn.Hex, 11).WithY(lastRow));
            Assert.Equal(HexSelection.At(0x63, HexColumn.Hex), tab.HexSelection);

            Click(window, At(window, view, 0, HexColumn.Text, 11).WithY(lastRow));
            Assert.Equal(HexSelection.At(0x63, HexColumn.Text), tab.HexSelection);
            window.Close();
        });

    [Fact]
    public void TheKeys_StillMoveTheCurrentRow_AndKeepTheSelection()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            Click(window, At(window, view, 1, HexColumn.Hex, 0));
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, tab.HexRow);
            Assert.Equal(HexSelection.At(0x10, HexColumn.Hex), tab.HexSelection);
            window.Close();
        });

    [Fact]
    public void DraggingBelowTheRows_ScrollsThemAndExtendsTheSelection()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("big.bin", new byte[64 * 1024])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            ListBox rows = view.RowList;
            int onScreen = (int)(rows.Bounds.Height / Row(view, 0).Bounds.Height);
            Point below = rows.TranslatePoint(new Point(rows.Bounds.Width / 3, rows.Bounds.Height + 10), window)!.Value;

            window.MouseDown(At(window, view, 0, HexColumn.Hex, 0), MouseButton.Left);

            for (int move = 0; move < onScreen + 20; move++)
            {
                window.MouseMove(below.WithY(below.Y + (move % 2)), RawInputModifiers.LeftMouseButton);
                Dispatcher.UIThread.RunJobs();
            }

            window.MouseUp(below, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            HexSelection selection = tab.HexSelection!.Value;
            Assert.Equal(0, selection.Anchor);
            Assert.True(selection.Active / 16 > onScreen + 10, $"reached row {selection.Active / 16} with {onScreen} on screen");
            Assert.Equal(HexColumn.Hex, selection.Column);
            Assert.Equal((int)(selection.Active / 16), tab.HexRow);
            // The rows scrolled: the first one is above the top, when it is realized at all.
            Assert.True((rows.ContainerFromIndex(0) is not { } first) || (first.TranslatePoint(default, rows)!.Value.Y < 0));
            window.Close();
        });

    private static void Click(MainWindow window, Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    // The middle of a byte's first hex digit, or of its character, in window coordinates.
    private static Point At(MainWindow window, HexView view, int row, HexColumn column, int byteInRow)
    {
        HexRowBlock block = Row(view, 0);
        int character = column == HexColumn.Hex
            ? HexFormatter.HexCharIndex(byteInRow, block.OffsetDigits)
            : HexFormatter.TextCharIndex(byteInRow, block.OffsetDigits);
        Point inRow = CharacterCentre(window, block, character);
        HexRowBlock target = Row(view, row);

        return inRow.WithY(target.TranslatePoint(new Point(0, target.Bounds.Height / 2), window)!.Value.Y);
    }

    private static Point CharacterCentre(MainWindow window, HexRowBlock row, int character)
    {
        double left = row.TextLayout.HitTestTextPosition(character).X;
        double right = row.TextLayout.HitTestTextPosition(character + 1).X;

        return row.TranslatePoint(new Point(row.Padding.Left + ((left + right) / 2), row.Bounds.Height / 2), window)!.Value;
    }

    private static HexRowBlock Row(HexView view, int index)
    {
        Control container = view.RowList.ContainerFromIndex(index) ?? throw new Xunit.Sdk.XunitException($"Row {index} is not realized.");

        return container.GetVisualDescendants().OfType<HexRowBlock>().Single();
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
