using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Clicks on the text with vim on, through the real window (BUG-47C2).
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class VimPointerViewTests(HeadlessAvaloniaFixture fixture)
{
    private const string Text = "abc\ndefgh\n";

    [Fact]
    public void AClickPastTheEndOfALine_InNormalMode_PutsTheBlockOnItsLastCharacter()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            Click(window, PastTheEndOf(window, line: 1));

            Assert.Equal(2, Editor(window).CaretOffset);
            Assert.Equal("NORMAL", ModeText(window));

            window.KeyTextInput("x");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("ab\ndefgh\n", Editor(window).Document.Text);
            window.Close();
        });

    [Fact]
    public void AClickPastTheEndOfALine_InInsertMode_LeavesTheCaretAfterIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            window.KeyTextInput("i");

            Click(window, PastTheEndOf(window, line: 1));

            Assert.Equal(3, Editor(window).CaretOffset);
            Assert.Equal("INSERT", ModeText(window));
            window.Close();
        });

    [Fact]
    public void AClick_EndsAVisualMode()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            window.KeyTextInput("vl");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("VISUAL", ModeText(window));

            Click(window, PastTheEndOf(window, line: 2));

            Assert.Equal("NORMAL", ModeText(window));
            Assert.True(Editor(window).TextArea.Selection.IsEmpty);
            Assert.Equal(8, Editor(window).CaretOffset);
            window.Close();
        });

    [Fact]
    public void WithVimOff_AClickPastTheEndOfALine_LeavesTheCaretAfterIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            Click(window, PastTheEndOf(window, line: 1));

            Assert.Equal(3, Editor(window).CaretOffset);
            window.Close();
        });

    /// <summary>
    /// A point on a line, well to the right of its last character, in the window's coordinates.
    /// </summary>
    internal static Point PastTheEndOf(MainWindow window, int line)
    {
        AvaloniaEdit.TextEditor editor = Editor(window);
        TextView view = editor.TextArea.TextView;
        view.EnsureVisualLines();

        int length = editor.Document.GetLineByNumber(line).Length;
        Point end = view.GetVisualPosition(new TextViewPosition(line, length + 1), VisualYPosition.LineMiddle) - view.ScrollOffset;

        return view.TranslatePoint(end + new Point(40, 0), window)!.Value;
    }

    /// <summary>
    /// A point in the middle of a character, in the window's coordinates.
    /// </summary>
    internal static Point On(MainWindow window, int line, int column)
    {
        TextView view = Editor(window).TextArea.TextView;
        view.EnsureVisualLines();

        Point left = view.GetVisualPosition(new TextViewPosition(line, column), VisualYPosition.LineMiddle) - view.ScrollOffset;
        Point right = view.GetVisualPosition(new TextViewPosition(line, column + 1), VisualYPosition.LineMiddle) - view.ScrollOffset;

        return view.TranslatePoint(new Point((left.X + right.X) / 2, left.Y), window)!.Value;
    }

    internal static void Click(MainWindow window, Point point)
    {
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    internal static string? ModeText(MainWindow window) => window.FindControl<TextBlock>("ModeText")?.Text;

    internal static AvaloniaEdit.TextEditor Editor(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl;

    internal static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
