using Enigma.TextEditor.Core;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// What the About dialog says: the product, its version, the build it was cut from and the copyright.
/// </summary>
public sealed class AboutViewModel
{
    /// <summary>
    /// Gets the product's name.
    /// </summary>
    public string DisplayName => AppInfo.DisplayName;

    /// <summary>
    /// Gets the product version.
    /// </summary>
    public string Version => AppInfo.Version;

    /// <summary>
    /// Gets the short revision the build was cut from, or <see langword="null"/> when it recorded none.
    /// </summary>
    public string? BuildSha => AppInfo.BuildSha;

    /// <summary>
    /// Gets a value indicating whether there is a build revision to show.
    /// </summary>
    public bool HasBuild => BuildSha is not null;

    /// <summary>
    /// Gets the copyright notice.
    /// </summary>
    public string Copyright => AppInfo.Copyright;
}
