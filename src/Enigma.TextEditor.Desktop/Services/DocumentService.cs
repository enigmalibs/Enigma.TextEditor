using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Reads and writes buffers through <see cref="DocumentFile"/>, keeping <see cref="DocumentRegistry"/>
/// the one place a file's buffer is found.
/// </summary>
public sealed class DocumentService : IDocumentService
{
    private readonly DocumentRegistry _registry;
    private readonly IFilePrompts _prompts;
    private readonly IMessageService _messages;
    private readonly IConfig<AppSettings> _settings;
    private readonly LanguageCatalog _languages;
    private readonly IBinaryFilePrompt _binary;
    private readonly ILogger<DocumentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentService"/> class.
    /// </summary>
    /// <param name="registry">The open buffers.</param>
    /// <param name="prompts">The open and save pickers.</param>
    /// <param name="messages">Where failures are reported.</param>
    /// <param name="settings">The defaults a new or inconclusive file gets.</param>
    /// <param name="languages">The language a file's name implies.</param>
    /// <param name="binary">Asks how to open a file that looks binary or is too large.</param>
    /// <param name="logger">Where failures are logged.</param>
    public DocumentService(
        DocumentRegistry registry,
        IFilePrompts prompts,
        IMessageService messages,
        IConfig<AppSettings> settings,
        LanguageCatalog languages,
        IBinaryFilePrompt binary,
        ILogger<DocumentService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _languages = languages ?? throw new ArgumentNullException(nameof(languages));
        _binary = binary ?? throw new ArgumentNullException(nameof(binary));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the indentation a file gets when its own does not say, from the settings.
    /// </summary>
    private Indentation DefaultIndentation => _settings.Value.IndentWithTabs
        ? Indentation.Tabs(_settings.Value.IndentSize)
        : Indentation.Spaces(_settings.Value.IndentSize);

    /// <inheritdoc />
    public async Task<DocumentViewModel?> OpenAsync(string filePath, bool forceText = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (_registry.Find(filePath) is { } open)
        {
            return open;
        }

        // Already chosen: a file open in the hex viewer and not as text is shown there again, without
        // the question it was opened through.
        if (!forceText && (_registry.FindHex(filePath) is { } hexOpen))
        {
            return hexOpen;
        }

        string fullPath = Path.GetFullPath(filePath);

        try
        {
            if (!forceText)
            {
                FileProbe probe = await DocumentFile.ProbeAsync(fullPath);
                string name = Path.GetFileName(fullPath);

                if (probe.IsTooLargeForText)
                {
                    return await _binary.AskTooLargeAsync(name, HexDocument.FormatSize(probe.Length))
                        ? await OpenHexAsync(fullPath)
                        : null;
                }

                if (probe.LooksBinary)
                {
                    switch (await _binary.AskBinaryAsync(name))
                    {
                        case BinaryFileChoice.Hex:
                            return await OpenHexAsync(fullPath);

                        case BinaryFileChoice.Cancel:
                            return null;

                        default:
                            break;
                    }
                }
            }

            DocumentContent content = await DocumentFile.ReadAsync(fullPath);

            // Checked again after the await: the same file opened twice in quick succession — two
            // startup arguments naming it — must still end up as one buffer.
            if (_registry.Find(fullPath) is { } raced)
            {
                return raced;
            }

            Indentation indentation = IndentationDetector.Detect(content.Text, DefaultIndentation);

            DocumentViewModel document = new(fullPath, _registry.NextUntitledName(), content, indentation);
            document.DetectLanguage(_languages.ForFile(fullPath));

            return _registry.Add(document);
        }
        catch (Exception failure) when (IsFileFailure(failure))
        {
            _logger.LogError(failure, "{FilePath} could not be opened.", fullPath);
            await _messages.ShowErrorAsync("This file could not be opened", $"{Path.GetFileName(fullPath)} — {failure.Message}");

            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentViewModel?> OpenHexAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (_registry.FindHex(filePath) is { } open)
        {
            return open;
        }

        string fullPath = Path.GetFullPath(filePath);

        try
        {
            HexDocument hex = HexDocument.Open(fullPath);
            DocumentContent empty = new(string.Empty, TextFileFormat.Default);

            return _registry.Add(new DocumentViewModel(fullPath, _registry.NextUntitledName(), empty, Indentation.Default, hex));
        }
        catch (Exception failure) when (IsFileFailure(failure))
        {
            _logger.LogError(failure, "{FilePath} could not be opened in the hex viewer.", fullPath);
            await _messages.ShowErrorAsync("This file could not be opened", $"{Path.GetFileName(fullPath)} — {failure.Message}");

            return null;
        }
    }

    /// <inheritdoc />
    public DocumentViewModel CreateUntitled()
        => _registry.CreateUntitled(TextFileFormat.Default.With(_settings.Value.DefaultLineEnding), DefaultIndentation);

    /// <inheritdoc />
    public DocumentViewModel CreateForMissingFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (_registry.Find(filePath) is { } open)
        {
            return open;
        }

        DocumentContent empty = new(string.Empty, TextFileFormat.Default.With(_settings.Value.DefaultLineEnding));

        DocumentViewModel document = new(Path.GetFullPath(filePath), _registry.NextUntitledName(), empty, DefaultIndentation);
        document.DetectLanguage(_languages.ForFile(filePath));

        return _registry.Add(document);
    }

    /// <inheritdoc />
    public Task<bool> SaveAsync(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // The hex view is read-only: there is nothing of its own to write.
        if (document.IsHex)
        {
            return Task.FromResult(false);
        }

        return document.FilePath is { } path ? WriteAsync(document, path) : SaveAsAsync(document);
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsAsync(DocumentViewModel document, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.IsHex)
        {
            return false;
        }

        string? target = filePath ?? await _prompts.AskWhereToSaveAsync(
            document.Title,
            document.FilePath is { } current ? Path.GetDirectoryName(current) : null);

        if (target is null)
        {
            return false;
        }

        string fullPath = Path.GetFullPath(target);

        if ((_registry.Find(fullPath) is { } other) && !ReferenceEquals(other, document))
        {
            await _messages.ShowErrorAsync(
                "This file could not be saved",
                $"{Path.GetFileName(fullPath)} is open in another tab. Close it there first, or save under another name.");

            return false;
        }

        return await WriteAsync(document, fullPath);
    }

    /// <inheritdoc />
    public async Task<bool> ReopenWithEncodingAsync(DocumentViewModel document, TextEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(encoding);

        if (document.FilePath is not { } path)
        {
            return false;
        }

        try
        {
            DocumentContent content = await DocumentFile.ReadAsync(path, encoding);
            document.ApplyReload(content);

            return true;
        }
        catch (Exception failure) when (IsFileFailure(failure))
        {
            _logger.LogError(failure, "{FilePath} could not be reopened as {Encoding}.", path, encoding.Label);
            await _messages.ShowErrorAsync("This file could not be reopened", $"{Path.GetFileName(path)} — {failure.Message}");

            return false;
        }
    }

    private static bool IsFileFailure(Exception failure)
        => failure is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException
            or UnrepresentableTextException;

    private async Task<bool> WriteAsync(DocumentViewModel document, string fullPath)
    {
        try
        {
            // Read on this thread: the document belongs to the thread that built it. The version is
            // taken with the text, because a keystroke can land while the write is in flight.
            string text = document.Document.Text;
            ITextSourceVersion written = document.Document.Version;
            TextFileFormat format = document.Format;

            await DocumentFile.WriteAsync(fullPath, text, format);

            document.MarkSaved(fullPath, written, format);

            // A Save As to another extension changes what the file is, unless the user said otherwise.
            document.DetectLanguage(_languages.ForFile(fullPath));

            return true;
        }
        catch (Exception failure) when (IsFileFailure(failure))
        {
            _logger.LogError(failure, "{FilePath} could not be written.", fullPath);
            await _messages.ShowErrorAsync("This file could not be saved", $"{Path.GetFileName(fullPath)} — {failure.Message}");

            return false;
        }
    }
}
