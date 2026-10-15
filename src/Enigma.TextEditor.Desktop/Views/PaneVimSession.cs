using System;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The Vim session as one pane's engine sees it: reports go through only while the pane is active.
/// </summary>
/// <remarks>
/// Every pane has an engine, and the status bar has one mode chip. An inactive pane's engine can still
/// report — a reset on a tab switch, a layout rebuild — and would otherwise overwrite the mode of the
/// pane the user is typing in. What it reads (whether Vim is on) is the real session's, so every pane
/// attaches and detaches its engine together.
/// </remarks>
internal sealed class PaneVimSession : IVimSession
{
    private readonly IVimSession _session;
    private readonly Func<bool> _isActive;

    public PaneVimSession(IVimSession session, Func<bool> isActive)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _isActive = isActive ?? throw new ArgumentNullException(nameof(isActive));
    }

    public event EventHandler? Changed
    {
        add => _session.Changed += value;
        remove => _session.Changed -= value;
    }

    public bool IsEnabled => _session.IsEnabled;

    public VimMode Mode => _session.Mode;

    public string? StatusMessage => _session.StatusMessage;

    public string? CommandLine => _session.CommandLine;

    public void OnSettingsChanged() => _session.OnSettingsChanged();

    public void ReportMode(VimMode mode)
    {
        if (_isActive())
        {
            _session.ReportMode(mode);
        }
    }

    public void ReportStatus(string? message)
    {
        if (_isActive())
        {
            _session.ReportStatus(message);
        }
    }

    public void ReportCommandLine(string? line)
    {
        if (_isActive())
        {
            _session.ReportCommandLine(line);
        }
    }
}
