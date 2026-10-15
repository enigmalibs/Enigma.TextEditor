using System;
using Enigma.TextEditor.Core.Vim;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The buffer the hex view's Vim engine works on: no text at all.
/// </summary>
/// <remarks>
/// The hex view is read-only and moves through rows, not characters, so none of Vim's text commands
/// apply. What it does want from the engine is the rest of the keyboard: the <c>:</c> line —
/// <c>:q</c>, <c>:bn</c>, <c>:sp</c> — and <c>Ctrl+W</c>. An engine over an empty buffer gives exactly
/// that: its text commands find nothing to act on, and its requests leave as they do from an editor.
/// </remarks>
internal sealed class HexVimBuffer : IVimBuffer
{
    private static readonly VimLine Empty = new(1, 0, string.Empty, 0);

    private readonly Action<string?> _status;
    private readonly Action<string?> _commandLine;

    public HexVimBuffer(Action<string?> status, Action<string?> commandLine)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
    }

    // Never raised: nothing here changes.
    public event EventHandler<VimTextChange>? Changed
    {
        add { }
        remove { }
    }

    public int TextLength => 0;

    public int LineCount => 1;

    public int CaretOffset
    {
        get => 0;
        set { }
    }

    public string NewLine => "\n";

    public int FirstVisibleLine => 1;

    public int VisibleLineCount => 1;

    public char CharAt(int offset) => '\0';

    public string GetText(int offset, int length) => string.Empty;

    public VimLine GetLine(int lineNumber) => Empty;

    public VimLine GetLineByOffset(int offset) => Empty;

    public void Replace(int offset, int length, string text)
    {
    }

    public void BeginUndoGroup()
    {
    }

    public void EndUndoGroup()
    {
    }

    public void Undo()
    {
    }

    public void Redo()
    {
    }

    public void SetSelection(int start, int end)
    {
    }

    public void ClearSelection()
    {
    }

    public void ScrollToCaret()
    {
    }

    public void ScrollToLine(int lineNumber, VimScrollAlignment alignment)
    {
    }

    public void ShowStatus(string? message) => _status(message);

    public void ShowCommandLine(string? line) => _commandLine(line);
}
