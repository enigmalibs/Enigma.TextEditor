using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Bytes selected in the hex view: painted in the column they were selected in, mirrored in the other, on the rows
/// on screen.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HexSelectionViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ASelectionOnTheTab_ReachesExactlyTheRowsItSpans()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[100])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;

            tab.HexSelection = new HexSelection(0x23, 0x0E, HexColumn.Hex);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal((14, 15), Row(view, 0).SelectedBytes);
            Assert.Equal((0, 15), Row(view, 1).SelectedBytes);
            Assert.Equal((0, 3), Row(view, 2).SelectedBytes);
            Assert.Null(Row(view, 3).SelectedBytes);
            Assert.Equal(tab.HexSelection, Row(view, 1).Selection);

            // The last row holds four bytes (0x60 to 0x63): a selection reaching it stops at its last.
            tab.HexSelection = new HexSelection(0x5E, 0x63, HexColumn.Text);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal((14, 15), Row(view, 5).SelectedBytes);
            Assert.Equal((0, 3), Row(view, 6).SelectedBytes);
            Assert.Equal(4, Row(view, 6).ByteCount);
            Assert.Null(Row(view, 0).SelectedBytes);
            window.Close();
        });

    [Fact]
    public void TheBands_CoverTheSelectedPairsAndCharacters_AndTheSpacesBetweenThePairs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[64])))!;
            MainWindow window = Show(harness);
            HexRowBlock row = Row(Pane(window).HexView, 1);

            tab.HexSelection = new HexSelection(0x16, 0x19, HexColumn.Hex);
            Dispatcher.UIThread.RunJobs();

            (Rect hex, Rect text) = row.SelectionBands!.Value;

            // Bytes 6 to 9 of the row: from the first digit of 06 to the last of 09, the gap between the groups
            // included; and their four characters.
            Assert.Equal(X(row, HexFormatter.HexCharIndex(6, 8)), hex.Left, 3);
            Assert.Equal(X(row, HexFormatter.HexCharIndex(9, 8) + 2), hex.Right, 3);
            Assert.Equal(X(row, HexFormatter.TextCharIndex(6, 8)), text.Left, 3);
            Assert.Equal(X(row, HexFormatter.TextCharIndex(9, 8) + 1), text.Right, 3);
            Assert.Equal(row.Bounds.Height, hex.Height);
            Assert.True(text.Left > hex.Right);
            window.Close();
        });

    [Fact]
    public void TheSelectionsColumn_IsPaintedFull_AndTheOtherColumnFainter()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[128])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            Assert.True(window.TryFindResource("EnigmaSelectionBrush", out object? selection));
            Assert.Same(selection, Row(view, 2).SelectionBrush);

            tab.HexSelection = new HexSelection(0x20, 0x2F, HexColumn.Hex);
            Dispatcher.UIThread.RunJobs();
            (Rect hex, Rect text) = Row(view, 2).SelectionBands!.Value;

            using (Frame frame = Frame.Capture(window))
            {
                uint hexSelected = frame.At(Row(view, 2), new Point(hex.Center.X, 1));
                uint textSelected = frame.At(Row(view, 2), new Point(text.Center.X, 1));
                uint hexPlain = frame.At(Row(view, 4), new Point(hex.Center.X, 1));
                uint textPlain = frame.At(Row(view, 4), new Point(text.Center.X, 1));

                Assert.NotEqual(hexPlain, hexSelected);
                Assert.NotEqual(textPlain, textSelected);
                Assert.NotEqual(hexSelected, textSelected);
            }

            // Made in the text column, the full colour moves there.
            tab.HexSelection = new HexSelection(0x20, 0x2F, HexColumn.Text);
            Dispatcher.UIThread.RunJobs();

            using (Frame frame = Frame.Capture(window))
            {
                uint hexMirror = frame.At(Row(view, 2), new Point(hex.Center.X, 1));
                uint textFull = frame.At(Row(view, 2), new Point(text.Center.X, 1));
                uint textPlain = frame.At(Row(view, 4), new Point(text.Center.X, 1));

                Assert.NotEqual(textPlain, textFull);
                Assert.NotEqual(hexMirror, textFull);
            }

            window.Close();
        });

    [Fact]
    public void SwitchingTabsAndBack_KeepsEachTabsSelection()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel first = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("a.bin", new byte[64])))!;
            EditorTabViewModel second = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("b.bin", new byte[64])))!;
            MainWindow window = Show(harness);
            HexView view = Pane(window).HexView;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;

            pane.ActiveTab = first;
            Dispatcher.UIThread.RunJobs();
            first.HexSelection = new HexSelection(2, 5, HexColumn.Text);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal((2, 5), Row(view, 0).SelectedBytes);

            pane.ActiveTab = second;
            Dispatcher.UIThread.RunJobs();
            Assert.Null(Row(view, 0).SelectedBytes);

            pane.ActiveTab = first;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal((2, 5), Row(view, 0).SelectedBytes);
            Assert.Equal(HexColumn.Text, Row(view, 0).Selection?.Column);
            window.Close();
        });

    [Fact]
    public void TwoPanesOnOneFile_EachHaveTheirOwnSelection()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("data.bin", new byte[64]);
            EditorTabViewModel left = (await harness.Workspace.OpenHexAsync(path))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            EditorTabViewModel right = (await harness.Workspace.OpenHexAsync(path))!;
            MainWindow window = Show(harness);
            EditorPaneView[] panes = [.. window.GetVisualDescendants().OfType<EditorPaneView>()];
            HexView leftView = panes.Single(view => ReferenceEquals(view.DataContext, leftPane)).HexView;
            HexView rightView = panes.Single(view => !ReferenceEquals(view.DataContext, leftPane)).HexView;

            Assert.NotSame(left, right);
            Assert.Same(left.Document, right.Document);

            left.HexSelection = new HexSelection(0, 3, HexColumn.Hex);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal((0, 3), Row(leftView, 0).SelectedBytes);
            Assert.Null(Row(rightView, 0).SelectedBytes);
            window.Close();
        });

    private static double X(HexRowBlock row, int charIndex) => row.Padding.Left + row.TextLayout.HitTestTextPosition(charIndex).X;

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

    // A rendered frame of the window, read pixel by pixel.
    private sealed class Frame : System.IDisposable
    {
        private readonly MainWindow _window;
        private readonly WriteableBitmap _bitmap;
        private readonly ILockedFramebuffer _buffer;

        private Frame(MainWindow window, WriteableBitmap bitmap)
        {
            _window = window;
            _bitmap = bitmap;
            _buffer = bitmap.Lock();
        }

        public static Frame Capture(MainWindow window)
        {
            Dispatcher.UIThread.RunJobs();

            return new Frame(window, window.CaptureRenderedFrame() ?? throw new Xunit.Sdk.XunitException("No frame was rendered."));
        }

        // The pixel at a point of a control, as 0xAARRGGBB or however the frame lays its bytes out: only compared.
        public uint At(Visual control, Point point)
        {
            Point inWindow = control.TranslatePoint(point, _window) ?? throw new Xunit.Sdk.XunitException("Not in the window.");
            double scale = _bitmap.PixelSize.Width / _window.Bounds.Width;
            int x = (int)(inWindow.X * scale);
            int y = (int)(inWindow.Y * scale);

            return (uint)Marshal.ReadInt32(_buffer.Address, (y * _buffer.RowBytes) + (x * 4));
        }

        public void Dispose()
        {
            _buffer.Dispose();
            _bitmap.Dispose();
        }
    }
}
