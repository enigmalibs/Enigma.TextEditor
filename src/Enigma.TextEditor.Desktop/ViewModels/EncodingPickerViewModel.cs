using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.TextEditor.Core.Documents;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The encoding picker: the editor's encodings, with the buffer's current one selected.
/// </summary>
public sealed class EncodingPickerViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncodingPickerViewModel"/> class.
    /// </summary>
    /// <param name="encodings">The encodings offered.</param>
    /// <param name="current">The buffer's encoding.</param>
    public EncodingPickerViewModel(IReadOnlyList<TextEncoding> encodings, TextEncoding current)
    {
        Items = encodings ?? throw new ArgumentNullException(nameof(encodings));
        ArgumentNullException.ThrowIfNull(current);

        Selected = Items.FirstOrDefault(encoding => encoding.Equals(current)) ?? Items.FirstOrDefault();
    }

    /// <summary>
    /// Gets the encodings offered.
    /// </summary>
    public IReadOnlyList<TextEncoding> Items { get; }

    /// <summary>
    /// Gets or sets the highlighted encoding.
    /// </summary>
    public TextEncoding? Selected { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the encoding picked by a double click or Enter; <see langword="null"/> until then.
    /// </summary>
    public TextEncoding? Committed { get; private set; }

    /// <summary>
    /// Picks the highlighted encoding.
    /// </summary>
    public void Commit() => Committed = Selected;
}
