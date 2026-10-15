using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Settings;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Writes <see cref="AppSettings"/> through Enigma.Configuration, reporting a failure once.
/// </summary>
public sealed class SettingsWriter : ISettingsWriter
{
    private readonly IConfig<AppSettings> _settings;
    private readonly IMessageService _messages;
    private readonly ILogger<SettingsWriter> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _reported;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWriter"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="messages">Where a failure is reported.</param>
    /// <param name="logger">Where a failure is logged.</param>
    public SettingsWriter(IConfig<AppSettings> settings, IMessageService messages, ILogger<SettingsWriter> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync()
    {
        Exception failure;

        // One write at a time: two toggles in quick succession would otherwise race on the same file.
        await _gate.WaitAsync();

        try
        {
            await _settings.WriteAsync();

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or EnigmaConfigurationException)
        {
            failure = exception;
            _logger.LogError(failure, "The settings could not be written to {FilePath}.", _settings.FilePath);
        }
        finally
        {
            _gate.Release();
        }

        if (!_reported)
        {
            _reported = true;
            await _messages.ShowErrorAsync(
                "Settings could not be saved",
                $"Changes apply until the editor closes, but will not be remembered. {failure.Message}");
        }

        return false;
    }
}
