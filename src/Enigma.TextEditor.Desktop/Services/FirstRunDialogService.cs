using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The welcome dialog of a first start, in the window's content dialog, as Enigma.MarkdownEditor shows its
/// own.
/// </summary>
/// <remarks>
/// <para>
/// The choices are applied and written after the dialog has gone: the dialog host is a single control, so
/// a failed write reported while the welcome dialog was still up would replace it rather than appear
/// over it.
/// </para>
/// <para>
/// The dialog's result is not read. Every way out means the same here: apply what it shows, so that the
/// settings file whose absence raised the dialog now exists.
/// </para>
/// </remarks>
public sealed class FirstRunDialogService : IFirstRunDialogService
{
    // Resolved once: the dialog is configured in code, where {ei:IconGeometry} is not available.
    private static readonly Geometry Icon = PhosphorIconSet.Instance.GetGlyph(PhosphorIcon.HandWaving, PhosphorWeight.Regular).ToGeometry();

    private readonly IContentDialogService _dialogs;
    private readonly FirstRunViewModel _viewModel;
    private readonly ILogger<FirstRunDialogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstRunDialogService"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    /// <param name="viewModel">What the dialog shows, and applies when it closes.</param>
    /// <param name="logger">Records that a first run happened, and a failure of it.</param>
    public FirstRunDialogService(IContentDialogService dialogs, FirstRunViewModel viewModel, ILogger<FirstRunDialogService> logger)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        _logger.LogInformation("First run: no settings file, showing the welcome dialog.");

        try
        {
            FirstRunView view = new() { DataContext = _viewModel };

            await _dialogs.ShowAsync(dialog =>
            {
                dialog.Title = "Welcome";
                dialog.Content = view;
                dialog.IconData = Icon;
                dialog.PrimaryButtonText = "Get started";
                dialog.CloseButtonText = "Skip";
                dialog.DefaultButton = DefaultButton.Primary;
            });

            await _viewModel.CompleteAsync();
        }
        catch (Exception exception)
        {
            // Broad on purpose, and only here: whatever went wrong with the welcome, the editor has to
            // finish starting, and a write failure has already been reported by the settings writer.
            _logger.LogError(exception, "The welcome dialog failed; the editor starts with the settings as they are.");
        }
    }
}
