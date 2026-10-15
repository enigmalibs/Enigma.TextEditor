using System.Collections.Generic;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records the themes applied instead of repainting the shared test application.
/// </summary>
internal sealed class FakeThemeService : IThemeService
{
    public List<AppTheme> Applied { get; } = [];

    public void Apply(AppTheme theme) => Applied.Add(theme);
}
