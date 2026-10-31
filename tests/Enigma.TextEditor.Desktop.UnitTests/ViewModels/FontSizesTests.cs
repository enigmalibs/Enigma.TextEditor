using System.Linq;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The sizes the status bar offers, and how one reads in the list.
/// </summary>
public sealed class FontSizesTests
{
    [Fact]
    public void ThePresets_AreInOrder_AndWithinTheSettingsRange()
    {
        Assert.Equal(FontSizes.Presets.Order(), FontSizes.Presets);
        Assert.Equal(FontSizes.Presets.Count, FontSizes.Presets.Distinct().Count());
        Assert.All(FontSizes.Presets, size => Assert.InRange(size, AppSettings.MinimumFontSize, AppSettings.MaximumFontSize));
        Assert.Contains(AppSettings.DefaultFontSize, FontSizes.Presets);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(36)]
    public void APresetSize_AddsNothing(int current)
        => Assert.Equal(FontSizes.Presets, FontSizes.ListFor(current));

    [Theory]
    [InlineData(17)]
    [InlineData(AppSettings.MinimumFontSize)]
    [InlineData(AppSettings.MaximumFontSize)]
    public void ASizeOffTheList_IsAddedInItsPlace(int current)
    {
        int[] sizes = [.. FontSizes.ListFor(current)];

        Assert.Equal(FontSizes.Presets.Count + 1, sizes.Length);
        Assert.Contains(current, sizes);
        Assert.Equal(sizes.Order(), sizes);
    }

    [Theory]
    [InlineData(AppSettings.MinimumFontSize - 1)]
    [InlineData(AppSettings.MaximumFontSize + 1)]
    public void ASizeOutsideTheSettingsRange_IsNeverOffered(int current)
        => Assert.Equal(FontSizes.Presets, FontSizes.ListFor(current));

    [Fact]
    public void TheDefaultSize_SaysSo_AndTheOthersAreTheirNumber()
    {
        Assert.Equal("14 (default)", new FontSizeChoice(14, IsCurrent: false, IsDefault: true, NoCommand.Instance).Header);
        Assert.Equal("12", new FontSizeChoice(12, IsCurrent: true, IsDefault: false, NoCommand.Instance).Header);
    }

    private sealed class NoCommand : System.Windows.Input.ICommand
    {
        public static readonly NoCommand Instance = new();

        public event System.EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => false;

        public void Execute(object? parameter)
        {
        }
    }
}
