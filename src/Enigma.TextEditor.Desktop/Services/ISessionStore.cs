using Enigma.TextEditor.Core.Sessions;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Where the editor keeps what it showed when it closed, for the next start to bring back.
/// </summary>
/// <remarks>
/// Only the instance that owns the session restores and saves it (see <see cref="SessionLock"/>); for any other,
/// <see cref="Load"/> has nothing and <see cref="Save"/> writes nothing, and that instance works as if there were no
/// session at all. Every failure is logged here and comes back as a plain answer.
/// </remarks>
public interface ISessionStore
{
    /// <summary>
    /// Gets a value indicating whether this instance owns the session. Claimed on first use, and kept until the
    /// application ends.
    /// </summary>
    bool IsOwner { get; }

    /// <summary>
    /// Reads the session the editor last closed with.
    /// </summary>
    /// <returns>
    /// The session; or <see langword="null"/> when there is none, when it could not be used — moved aside and
    /// logged — or when another instance owns it.
    /// </returns>
    SessionState? Load();

    /// <summary>
    /// Writes a session over the last one.
    /// </summary>
    /// <param name="state">The session.</param>
    /// <returns>Whether it was written; <see langword="false"/> when it failed — logged — or another instance owns it.</returns>
    bool Save(SessionState state);
}
