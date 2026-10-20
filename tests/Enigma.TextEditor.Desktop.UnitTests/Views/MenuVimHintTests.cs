using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The Vim keys shown on the right of menu and context-menu items: which items have them, that each one
/// is something the editor reads, and that they show only while Vim mode is on.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class MenuVimHintTests(HeadlessAvaloniaFixture fixture)
{
    private const string WindowPrefix = "Ctrl+W ";

    private static readonly Dictionary<string, string> MainMenuHints = new(StringComparer.Ordinal)
    {
        ["_New"] = ":enew",
        ["_Open…"] = ":e",
        ["_Save"] = ":w",
        ["Save _As…"] = ":w {file}",
        ["Save A_ll"] = ":wa",
        ["_Close Tab"] = ":q",
        ["E_xit"] = ":qa",
        ["_Undo"] = "u",
        ["_Redo"] = "Ctrl+R",
        ["Select _All"] = "ggVG",
        ["Split _Right"] = "Ctrl+W v",
        ["Split _Down"] = "Ctrl+W s",
        ["_Close Pane"] = "Ctrl+W c",
        ["_Only This Pane"] = "Ctrl+W o",
        ["_Equalize Panes"] = "Ctrl+W =",
        ["Focus _Next Pane"] = "Ctrl+W w",
        ["Focus _Previous Pane"] = "Ctrl+W W",
        ["_Key Bindings"] = ":help",
    };

    private static readonly Dictionary<string, string> ContextMenuHints = new(StringComparer.Ordinal)
    {
        ["_Undo"] = "u",
        ["_Redo"] = "Ctrl+R",
        ["_Delete"] = "x",
        ["Select _All"] = "ggVG",
    };

    [Fact]
    public void TheMainMenu_HintsAtExactlyTheItemsWithAVimTwin()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);

            Dictionary<string, string> hinted = Hinted(MainMenuItems(window));

            Assert.Equal(MainMenuHints.OrderBy(pair => pair.Key, StringComparer.Ordinal), hinted.OrderBy(pair => pair.Key, StringComparer.Ordinal));
            window.Close();
        });

    [Fact]
    public void TheContextMenu_HintsAtExactlyTheItemsWithAVimTwin_AndNotAtTheClipboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            MainWindow window = Show(harness);

            Dictionary<string, string> hinted = Hinted(ContextMenuOf(window).Items.OfType<MenuItem>());

            Assert.Equal(ContextMenuHints.OrderBy(pair => pair.Key, StringComparer.Ordinal), hinted.OrderBy(pair => pair.Key, StringComparer.Ordinal));
            window.Close();
        });

    [Fact]
    public void EveryHint_IsSomethingTheEditorReads()
        => fixture.Run(() =>
        {
            foreach (string hint in MainMenuHints.Values.Concat(ContextMenuHints.Values))
            {
                if (hint.StartsWith(':'))
                {
                    Assert.True(ExCommandParser.Parse(hint[1..].Replace("{file}", "notes.txt", StringComparison.Ordinal)) is not null, $"'{hint}' is not an ex command.");
                }
                else if (hint.StartsWith(WindowPrefix, StringComparison.Ordinal))
                {
                    Assert.Equal(WindowPrefix.Length + 1, hint.Length);
                    Assert.True(VimWindowKeys.Map(VimKey.Typed(hint[^1])) is not null, $"'{hint}' is not a window command.");
                }
                else if (hint != "Ctrl+R")
                {
                    Assert.True(ReadsAsCommands(hint), $"'{hint}' is not a normal-mode command.");
                }
            }
        });

    [Fact]
    public void WithVimOn_AnOpenMenuShowsTheHints_RightAligned_BeforeTheShortcut()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            MenuItem file = TopLevel(window, "_File");
            file.Open();
            Dispatcher.UIThread.RunJobs();

            MenuItem save = Child(file, "_Save");
            TextBlock hint = HintOf(save);
            Assert.Equal(":w", hint.Text);
            Assert.True(hint.IsEffectivelyVisible);

            // The header keeps its mnemonic.
            AccessText header = save.GetVisualDescendants().OfType<AccessText>().Single();
            Assert.Equal("Save", header.Text?.Replace("_", string.Empty, StringComparison.Ordinal));
            Assert.Equal("S", header.AccessKey?.ToString(), ignoreCase: true);

            // Every hint ends at the same place on its row, before the shortcut column.
            double[] ends = [.. file.Items.OfType<MenuItem>().Where(IsHinted).Select(item => RightEdge(HintOf(item), item))];
            Assert.All(ends, end => Assert.Equal(ends[0], end, 0.5));
            TextBlock gesture = save.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "PART_InputGestureText");
            Assert.True(RightEdge(hint, save) <= gesture.TranslatePoint(default, save)!.Value.X);

            file.Close();
            window.Close();
        });

    [Fact]
    public void WithVimOff_NoHintShows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            MenuItem file = TopLevel(window, "_File");
            file.Open();
            Dispatcher.UIThread.RunJobs();
            TextBlock hint = HintOf(Child(file, "_Save"));
            Assert.True(hint.IsEffectivelyVisible);

            await harness.SettingsMenu.ToggleVimCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();

            Assert.False(harness.SettingsMenu.IsVimEnabled);
            Assert.False(hint.IsEffectivelyVisible);
            file.Close();
            window.Close();
        });

    [Fact]
    public void TheContextMenu_ShowsItsHints_WhileVimIsOn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            ContextMenu menu = ContextMenuOf(window);
            menu.Open(pane.EditorControl);
            Dispatcher.UIThread.RunJobs();

            TextBlock undo = HintOf(menu.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Undo"));

            Assert.Equal("u", undo.Text);
            Assert.True(undo.IsEffectivelyVisible);
            menu.Close();
            window.Close();
        });

    // Feeds the keys to the grammar in the mode each one leaves it in, the way the engine would.
    private static bool ReadsAsCommands(string keys)
    {
        KeySequenceParser parser = new();
        VimMode mode = VimMode.Normal;

        foreach (char key in keys)
        {
            VimParseResult result = parser.Feed(VimKey.Typed(key), mode);

            if (result.Status == VimParseStatus.Invalid)
            {
                return false;
            }

            if (result.Status == VimParseStatus.Complete)
            {
                mode = result.Command.Action switch
                {
                    VimAction.VisualCharacterwise => VimMode.Visual,
                    VimAction.VisualLinewise => VimMode.VisualLine,
                    _ => mode,
                };
            }
        }

        return !parser.HasPendingSequence;
    }

    private static Dictionary<string, string> Hinted(IEnumerable<MenuItem> items)
        => items
            .Where(IsHinted)
            .ToDictionary(item => (string)item.Header!, item => VimHint.GetKeys(item)!, StringComparer.Ordinal);

    private static bool IsHinted(MenuItem item) => VimHint.GetKeys(item) is { Length: > 0 } && item.Classes.Contains(VimHint.ItemClass);

    private static IEnumerable<MenuItem> MainMenuItems(MainWindow window)
        => window.FindControl<Menu>("MainMenu")!.GetLogicalDescendants().OfType<MenuItem>();

    private static ContextMenu ContextMenuOf(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl.ContextMenu!;

    private static MenuItem TopLevel(MainWindow window, string header)
        => window.FindControl<Menu>("MainMenu")!.Items.OfType<MenuItem>().Single(item => (string?)item.Header == header);

    private static MenuItem Child(MenuItem parent, string header)
        => parent.Items.OfType<MenuItem>().Single(item => (string?)item.Header == header);

    private static TextBlock HintOf(MenuItem item)
        => item.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Classes.Contains("vimHintKeys"));

    private static double RightEdge(Visual visual, Visual relativeTo)
        => visual.TranslatePoint(new Point(visual.Bounds.Width, 0), relativeTo)!.Value.X;

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
