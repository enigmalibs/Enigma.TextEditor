using System;
using System.Collections.Generic;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// A language a buffer can be coloured as.
/// </summary>
/// <param name="Id">The TextMate language id — <c>csharp</c>, <c>json</c>.</param>
/// <param name="DisplayName">What the status bar and the picker show — <c>C#</c>, <c>JSON</c>.</param>
/// <param name="Extensions">The file extensions the language is chosen for, dot included.</param>
/// <param name="ScopeName">The grammar's scope, or <see langword="null"/> for plain text, which is not coloured.</param>
public sealed record LanguageInfo(string Id, string DisplayName, IReadOnlyList<string> Extensions, string? ScopeName)
{
    /// <summary>
    /// Gets the language of a file nothing is known about: no colouring at all.
    /// </summary>
    public static LanguageInfo PlainText { get; } = new("plaintext", "Plain Text", [], null);

    /// <summary>
    /// Gets a value indicating whether the language colours anything.
    /// </summary>
    public bool IsPlainText => ScopeName is null;

    /// <inheritdoc />
    public bool Equals(LanguageInfo? other) => (other is not null) && string.Equals(Id, other.Id, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);
}
