using System.Text.RegularExpressions;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// What <c>incsearch</c> shows while a <c>/</c> or <c>?</c> is being typed: every match of the pattern
/// so far, and the one Enter would land on.
/// </summary>
/// <param name="Pattern">The pattern typed so far, compiled.</param>
/// <param name="MatchOffset">Where the match Enter would land on starts.</param>
/// <param name="MatchLength">How long it is.</param>
public sealed record VimIncSearch(Regex Pattern, int MatchOffset, int MatchLength);
