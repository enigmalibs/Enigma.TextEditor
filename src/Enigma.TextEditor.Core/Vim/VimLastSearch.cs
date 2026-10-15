namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// The last search: what was looked for, which way, and whether <c>smartcase</c> may read it.
/// </summary>
/// <param name="Pattern">The pattern, as typed or as taken off the text.</param>
/// <param name="Forward">Whether the search went towards the end of the buffer.</param>
/// <param name="SmartCase">
/// Whether <c>smartcase</c> applies: not for <c>*</c> and <c>#</c>, whose capitals were not typed.
/// </param>
internal readonly record struct VimLastSearch(string Pattern, bool Forward, bool SmartCase);
