using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The languages the editor can colour, from the grammars TextMateSharp ships, and the one a file is
/// coloured as by default.
/// </summary>
/// <remarks>
/// Built once: reading the grammar index parses a few hundred kilobytes of JSON, and nothing in it
/// changes while the application runs. Only languages that have a grammar are offered — the index
/// also lists languages that are only configuration — and Plain Text is always first.
/// </remarks>
public sealed class LanguageCatalog
{
    private readonly Dictionary<string, LanguageInfo> _byExtension = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LanguageInfo> _byId = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCatalog"/> class from TextMateSharp's
    /// bundled grammars.
    /// </summary>
    public LanguageCatalog()
    {
        RegistryOptions options = new(ThemeName.DarkPlus);
        List<LanguageInfo> languages = [];

        foreach (Language language in options.GetAvailableLanguages())
        {
            if ((language.Id is not { Length: > 0 } id)
                || _byId.ContainsKey(id)
                || (options.GetScopeByLanguageId(id) is not { Length: > 0 } scope))
            {
                continue;
            }

            List<string> extensions = [.. (language.Extensions ?? []).Where(extension => extension is { Length: > 0 })];
            LanguageInfo info = new(id, DisplayNameOf(language, id), extensions, scope);

            languages.Add(info);
            _byId[id] = info;

            foreach (string extension in extensions)
            {
                _byExtension.TryAdd(extension, info);
            }
        }

        All = [LanguageInfo.PlainText, .. languages.OrderBy(language => language.DisplayName, StringComparer.OrdinalIgnoreCase)];
        _byId[LanguageInfo.PlainText.Id] = LanguageInfo.PlainText;
    }

    /// <summary>
    /// Gets every language, Plain Text first and the rest by name.
    /// </summary>
    public IReadOnlyList<LanguageInfo> All { get; }

    /// <summary>
    /// The language a file is coloured as: by its extension, else Plain Text.
    /// </summary>
    /// <param name="filePath">The file, or <see langword="null"/> for an untitled buffer.</param>
    /// <returns>The language.</returns>
    public LanguageInfo ForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return LanguageInfo.PlainText;
        }

        return _byExtension.TryGetValue(Path.GetExtension(filePath), out LanguageInfo? byExtension)
            ? byExtension
            : LanguageInfo.PlainText;
    }

    /// <summary>
    /// Finds a language by its id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The language, or <see langword="null"/>.</returns>
    public LanguageInfo? Find(string id) => _byId.GetValueOrDefault(id);

    /// <summary>
    /// Finds a language by any name a user would type for it: its id, its display name or one of its
    /// extensions without the dot — <c>python</c>, <c>C#</c>, <c>cs</c> — ignoring case.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The language, or <see langword="null"/>.</returns>
    /// <remarks>
    /// Vim's <c>filetype</c> names are mostly the grammar ids (<c>python</c>, <c>json</c>) or the
    /// extension (<c>cs</c>, <c>sh</c>), so the three together read most of them without a table.
    /// </remarks>
    public LanguageInfo? Lookup(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return All.FirstOrDefault(language => string.Equals(language.Id, name, StringComparison.OrdinalIgnoreCase))
            ?? All.FirstOrDefault(language => string.Equals(language.DisplayName, name, StringComparison.OrdinalIgnoreCase))
            ?? _byExtension.GetValueOrDefault("." + name);
    }

    // The first alias is the name the language goes by in the grammar's own package — "C#", "JSON",
    // "Python" — where the id is a key. A language with no alias shows its id.
    private static string DisplayNameOf(Language language, string id)
        => language.Aliases?.FirstOrDefault(alias => alias is { Length: > 0 }) ?? id;
}
