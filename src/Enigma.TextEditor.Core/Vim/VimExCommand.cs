namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// An ex command the engine has recognised and is asking the application to carry out.
/// </summary>
/// <remarks>
/// The requests that leave the engine, all of them about files and tabs rather than about the text.
/// That is why they leave as a request rather than as a call on <see cref="IVimBuffer"/>: saving,
/// opening and closing are the application's business, and the engine knows nothing about tabs,
/// dialogs or what an unsaved document is supposed to ask before it goes. The two normal-mode
/// spellings, <c>ZZ</c> and <c>ZQ</c>, raise the same requests as their ex lines. A request's argument,
/// when it takes one, travels beside it in <see cref="VimExRequest"/>. Every spelling of each is in
/// <see cref="ExCommandParser.Definitions"/>; the brackets below mark the part vim lets the user leave
/// out.
/// </remarks>
public enum VimExCommand
{
    /// <summary>
    /// <c>:w</c> — save the document being edited.
    /// </summary>
    Write,

    /// <summary>
    /// <c>:q</c> — close it, through whatever the application asks before throwing work away.
    /// </summary>
    Quit,

    /// <summary>
    /// <c>:wq</c>, <c>:x</c>, <c>:wq!</c> and <c>ZZ</c> — save it, and close it only if the save
    /// worked.
    /// </summary>
    /// <remarks>
    /// One value rather than a <see cref="Write"/> followed by a <see cref="Quit"/>: the save is
    /// asynchronous, and two requests in a row would be a close racing a write of the same file.
    /// </remarks>
    WriteQuit,

    /// <summary>
    /// <c>:q!</c> and <c>ZQ</c> — close it and throw away whatever has not been saved, without
    /// asking.
    /// </summary>
    /// <remarks>
    /// A different request from <see cref="Quit"/> rather than a flag on it, because the whole
    /// point is what the application does <em>not</em> do: the unsaved-changes question is the one
    /// thing the user has said they do not want.
    /// </remarks>
    QuitDiscard,

    /// <summary>
    /// <c>:e[dit] {file}</c> — open a file in the current pane, or show it there
    /// when it is already open.
    /// </summary>
    Edit,

    /// <summary>
    /// <c>:ene[w]</c> — a new untitled buffer in a new tab of the current pane.
    /// </summary>
    /// <remarks>
    /// Vim's <c>:enew</c> with <c>hidden</c> set: the buffer that was showing stays open, here in its
    /// own tab, as it does after <c>:e {file}</c>.
    /// </remarks>
    EditNew,

    /// <summary>
    /// <c>:ene[w]!</c> — a new untitled buffer in the current tab's place, throwing away what has not
    /// been saved of the buffer it replaces, without asking.
    /// </summary>
    /// <remarks>
    /// A different request from <see cref="EditNew"/> for the reason <see cref="QuitDiscard"/> is one:
    /// the point is the question the application does not ask. A buffer another tab still shows loses
    /// nothing.
    /// </remarks>
    EditNewDiscard,

    /// <summary>
    /// <c>:bn[ext]</c> — show the next tab.
    /// </summary>
    BufferNext,

    /// <summary>
    /// <c>:bp[revious]</c> — show the previous tab.
    /// </summary>
    BufferPrevious,

    /// <summary>
    /// <c>:wa[ll]</c> — save every buffer with unsaved changes.
    /// </summary>
    WriteAll,

    /// <summary>
    /// <c>:qa[ll]</c> — quit the application, through the unsaved-changes questions.
    /// </summary>
    QuitAll,

    /// <summary>
    /// <c>:qa[ll]!</c> — quit the application and throw away whatever has not been
    /// saved, without asking.
    /// </summary>
    QuitAllDiscard,

    /// <summary>
    /// <c>:sp[lit]</c> — split the pane, one above the other; with a file, open it in the
    /// new pane.
    /// </summary>
    Split,

    /// <summary>
    /// <c>:vs[plit]</c> — split the pane side by side; with a file, open it in the new
    /// pane.
    /// </summary>
    VerticalSplit,

    /// <summary>
    /// <c>:new</c> — split one above the other, with a new untitled buffer in the new pane.
    /// </summary>
    New,

    /// <summary>
    /// <c>:vne[w]</c> — split side by side, with a new untitled buffer in the new pane.
    /// </summary>
    VerticalNew,

    /// <summary>
    /// <c>:on[ly]</c> — close every other pane.
    /// </summary>
    Only,

    /// <summary>
    /// <c>:clo[se]</c> — close the pane.
    /// </summary>
    Close,

    /// <summary>
    /// <c>:h[elp]</c> — show the key bindings.
    /// </summary>
    /// <remarks>
    /// Whatever follows the name is accepted and not read: there is one help, not one per subject, and
    /// showing it is the useful answer to <c>:help vsplit</c>.
    /// </remarks>
    Help,
}
