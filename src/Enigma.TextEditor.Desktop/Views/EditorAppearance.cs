using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using Enigma.TextEditor.Desktop.Services;
using TextMateSharp.Grammars;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;
using TextMateInstallation = AvaloniaEdit.TextMate.TextMate.Installation;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// Paints an editor: its chrome — text, line numbers, caret, selection, current line and links — from
/// the Enigma palette, and its text from the TextMate grammar of the buffer's language.
/// </summary>
/// <remarks>
/// <para>
/// Assigned rather than bound: AvaloniaEdit's brushes are plain properties, so nothing moves them when
/// the variant changes, and <see cref="Apply"/> has to be called again. The token colours follow the
/// variant too, from Visual Studio Code's Dark+ and Light+ themes.
/// </para>
/// <para>
/// Ported from Enigma.MarkdownEditor's <c>EditorAppearance</c>, which coloured every buffer as
/// markdown. Here the grammar is the buffer's language, set again on every tab switch; Plain Text has
/// no grammar and is not coloured.
/// </para>
/// </remarks>
public sealed class EditorAppearance : IDisposable
{
    private const string ForegroundKey = "EnigmaForegroundBrush";
    private const string LineNumbersKey = "EnigmaForegroundTertiaryBrush";
    private const string SelectionKey = "EnigmaSelectionBrush";
    private const string CurrentLineKey = "EnigmaHoverBrush";

    // The app's own, not the palette's: App.axaml defines it per variant.
    private const string LinkKey = "EditorLinkBrush";

    // Fill only: the line the caret is on is a hint, not a box drawn round the text.
    private static readonly Pen NoBorder = new(Brushes.Transparent, 0);

    private readonly AvaloniaTextEditor _editor;
    private readonly RegistryOptions _registry;
    private readonly TextMateInstallation _installation;

    private ThemeName _themeApplied;
    private string? _scopeApplied;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorAppearance"/> class and paints the editor.
    /// </summary>
    /// <param name="editor">The editor.</param>
    /// <param name="variant">The variant to paint it in.</param>
    /// <param name="failureHandler">
    /// Told when the colouring fails, so it can be reported — not when the editor merely has no document.
    /// </param>
    public EditorAppearance(AvaloniaTextEditor editor, ThemeVariant variant, Action<Exception> failureHandler)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentNullException.ThrowIfNull(failureHandler);

        // The variant is handed to the registry rather than corrected afterwards: the installation
        // applies the registry's theme as it is built, and starting on the wrong one would paint the
        // editor dark for a frame in a light window.
        _themeApplied = ThemeNameFor(variant);
        _registry = new RegistryOptions(_themeApplied);
        _installation = _editor.InstallTextMate(
            _registry,
            initCurrentDocument: true,
            failure =>
            {
                if (!IsDocumentless(failure))
                {
                    failureHandler(failure);
                }
            });

        ApplyChrome(variant);
    }

    /// <summary>
    /// Gets the scope of the grammar the editor is coloured with, or <see langword="null"/> for none.
    /// </summary>
    public string? ScopeName => _scopeApplied;

    /// <summary>
    /// The TextMate theme for a variant.
    /// </summary>
    /// <param name="variant">The variant, resolved to light or dark.</param>
    /// <returns>Dark+ for dark, Light+ otherwise.</returns>
    public static ThemeName ThemeNameFor(ThemeVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);

        return ThemeVariant.Dark.Equals(variant) ? ThemeName.DarkPlus : ThemeName.LightPlus;
    }

    /// <summary>
    /// Colours the editor's text as a language.
    /// </summary>
    /// <param name="language">The language; Plain Text takes the colouring away.</param>
    public void SetLanguage(LanguageInfo language)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(language);

        if (string.Equals(_scopeApplied, language.ScopeName, StringComparison.Ordinal))
        {
            return;
        }

        _scopeApplied = language.ScopeName;
        _installation.SetGrammar(language.ScopeName);
    }

    /// <summary>
    /// Repaints the editor for a variant.
    /// </summary>
    /// <param name="variant">The variant, resolved to light or dark.</param>
    public void Apply(ThemeVariant variant)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(variant);

        ThemeName name = ThemeNameFor(variant);

        // Loading a theme parses it, and the variant can be re-announced without having changed.
        if (_themeApplied != name)
        {
            _installation.SetTheme(_registry.LoadTheme(name));
            _themeApplied = name;
        }

        ApplyChrome(variant);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Takes the line transformer off the text view and stops the background tokenizer.
        _installation.Dispose();
    }

    // The installation builds a model for whatever document the editor is given, and an editor whose
    // pane has no tab is given none: the model throws on it, and the installation hands that on as a
    // failure. Nothing is lost — the next document gets a model of its own — so it is not reported.
    // Checked on the UI thread only: the background tokenizer reports from its own thread, and the
    // editor must not be touched from there.
    private bool IsDocumentless(Exception failure)
        => (failure is ArgumentNullException { ParamName: "document" })
           && Dispatcher.UIThread.CheckAccess()
           && (_editor.Document is null);

    private void ApplyChrome(ThemeVariant variant)
    {
        Use(ForegroundKey, variant, brush => _editor.Foreground = brush);
        Use(LineNumbersKey, variant, brush => _editor.LineNumbersForeground = brush);
        Use(ForegroundKey, variant, brush => _editor.TextArea.CaretBrush = brush);
        Use(SelectionKey, variant, brush => _editor.TextArea.SelectionBrush = brush);
        Use(LinkKey, variant, brush => _editor.TextArea.TextView.LinkTextForegroundBrush = brush);

        Use(
            CurrentLineKey,
            variant,
            brush =>
            {
                _editor.TextArea.TextView.CurrentLineBackground = brush;
                _editor.TextArea.TextView.CurrentLineBorder = NoBorder;
            });
    }

    private void Use(string key, ThemeVariant variant, Action<IBrush> apply)
    {
        // A key that does not resolve leaves the property as it was rather than blanking it: the palette
        // comes from a package, and a brush renamed there should cost one wrong colour rather than an
        // invisible caret.
        if (_editor.TryFindResource(key, variant, out object? value) && (value is IBrush brush))
        {
            apply(brush);
        }
    }
}
