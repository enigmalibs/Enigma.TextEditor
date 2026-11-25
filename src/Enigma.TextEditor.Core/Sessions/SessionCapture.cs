using System.Collections.Generic;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// A session as captured to be written: the session itself, and the unsaved texts its buffers name, each still to be
/// written beside it unless it already is.
/// </summary>
/// <remarks>
/// Two sessions that are equal name the same texts by the same names — a name stands for one version of one buffer's
/// text — so comparing <see cref="State"/> says whether anything is worth writing.
/// </remarks>
/// <param name="State">The session.</param>
/// <param name="Texts">The texts it names (<see cref="SessionDocument.UnsavedTextFile"/>).</param>
public sealed record SessionCapture(SessionState State, IReadOnlyList<SessionText> Texts);
