using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The radio groups of the menus and the status bar as lists of choices: one checked per value in force, the same list
/// for the life of the menu, and the checks following whoever changed the value.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class MenuChoiceTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void EveryThemeChosen_ChecksItsOwnChoice_AndNoOther()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            foreach (AppTheme theme in Enum.GetValues<AppTheme>())
            {
                await harness.SettingsMenu.SetThemeAsync(theme);

                Assert.Equal(theme.ToString(), Current(harness.SettingsMenu.ThemeChoices));
            }
        });

    [Fact]
    public void ChoosingAValue_SetsIt_AndMovesTheCheck()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            SettingsMenuViewModel menu = harness.SettingsMenu;

            await menu.PaneResizeStepChoices.ChooseAsync("10%");
            await menu.DefaultIndentationChoices.ChooseAsync("Tabs");
            await menu.DefaultLineEndingChoices.ChooseAsync("CRLF");

            Assert.Equal(10, harness.Settings.Value.PaneResizeStep);
            Assert.True(harness.Settings.Value.IndentWithTabs);
            Assert.Equal(LineEndingStyle.CrLf, harness.Settings.Value.DefaultLineEnding);
            Assert.Equal("10%", Current(menu.PaneResizeStepChoices));
            Assert.Equal("Tabs", Current(menu.DefaultIndentationChoices));
            Assert.Equal("CRLF", Current(menu.DefaultLineEndingChoices));
        });

    [Fact]
    public void TheLists_AreTheSameForTheLifeOfTheMenu_AndTheirChecksSayWhenTheyChange()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            SettingsMenuViewModel menu = harness.SettingsMenu;
            IReadOnlyList<MenuChoice> steps = menu.PaneResizeStepChoices;
            MenuChoice two = steps.Choice("2%");
            List<string?> changed = [];
            two.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            await menu.SetPaneResizeStepAsync(2);
            await menu.ResetAsync();

            Assert.Same(steps, menu.PaneResizeStepChoices);
            Assert.Same(two, menu.PaneResizeStepChoices.Choice("2%"));
            Assert.False(two.IsCurrent);
            Assert.Equal([nameof(MenuChoice.IsCurrent), nameof(MenuChoice.IsCurrent)], changed);
        });

    [Fact]
    public void TheToolsChoices_FollowTheActiveBuffer_AndAreDisabledOnAHexTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("tabs.txt", "a\n\tb\n"));

            Assert.Equal("Tabs", Current(harness.Tools.IndentationChoices));
            Assert.All(harness.Tools.IndentationChoices, choice => Assert.True(choice.Command.CanExecute(null)));

            await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", new byte[] { 1, 2, 3 }));

            Assert.All(
                harness.Tools.IndentationChoices.Concat(harness.Tools.LineEndingChoices),
                choice => Assert.False(choice.Command.CanExecute(null)));
        });

    [Fact]
    public void EachGroup_HasOneRadioGroupName_OfItsOwn()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            IReadOnlyList<MenuChoice>[] groups =
            [
                harness.SettingsMenu.ThemeChoices,
                harness.SettingsMenu.PaneResizeStepChoices,
                harness.SettingsMenu.DefaultIndentationChoices,
                harness.SettingsMenu.DefaultLineEndingChoices,
                harness.Tools.LineEndingChoices,
                harness.Tools.IndentationChoices,
            ];

            // One group name per list, different from every other list's.
            Assert.All(groups, group => Assert.Single(group.Select(choice => choice.Group).Distinct()));
            Assert.Equal(groups.Length, groups.Select(group => group[0].Group).Distinct().Count());
        });

    // The header of the choice in force, its access-key underscore aside; none when no choice is.
    private static string? Current(IReadOnlyList<MenuChoice> choices)
        => choices.SingleOrDefault(choice => choice.IsCurrent)?.Header.Replace("_", string.Empty);
}
