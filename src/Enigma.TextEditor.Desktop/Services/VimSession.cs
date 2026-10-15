using System;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Core.Vim;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The application's one <see cref="IVimSession"/>, seeded from the settings.
/// </summary>
/// <remarks>
/// Ported from Enigma.MarkdownEditor. The engine's messages go to the shared
/// <see cref="StatusMessages"/> line rather than to a slot of their own, so the status bar has one
/// place for everything worth a line — a Vim message, a file that could not be opened.
/// </remarks>
public sealed class VimSession : IVimSession
{
    private readonly IConfig<AppSettings> _settings;
    private readonly StatusMessages _messages;

    /// <summary>
    /// Initializes a new instance of the <see cref="VimSession"/> class.
    /// </summary>
    /// <param name="settings">Read for <c>VimEnabled</c>, now and whenever the setting changes.</param>
    /// <param name="messages">Where the engine's messages are shown.</param>
    public VimSession(IConfig<AppSettings> settings, StatusMessages messages)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        IsEnabled = _settings.Value.VimEnabled;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public bool IsEnabled { get; private set; }

    /// <inheritdoc />
    public VimMode Mode { get; private set; } = VimMode.Normal;

    /// <inheritdoc />
    public string? StatusMessage => _messages.Current;

    /// <inheritdoc />
    public string? CommandLine { get; private set; }

    /// <inheritdoc />
    public void OnSettingsChanged()
    {
        if (IsEnabled == _settings.Value.VimEnabled)
        {
            return;
        }

        IsEnabled = _settings.Value.VimEnabled;

        // Switched off, the mode is not something the user can see or change any more, so it is put
        // back rather than left saying INSERT; a half-typed command line goes with it.
        Mode = VimMode.Normal;
        CommandLine = null;
        _messages.Show(null);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ReportMode(VimMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ReportStatus(string? message)
    {
        if (_messages.Current == message)
        {
            return;
        }

        _messages.Show(message);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ReportCommandLine(string? line)
    {
        if (CommandLine == line)
        {
            return;
        }

        CommandLine = line;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
