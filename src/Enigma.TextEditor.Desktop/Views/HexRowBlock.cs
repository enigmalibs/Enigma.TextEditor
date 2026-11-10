using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Enigma.TextEditor.Core.Hex;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// One row of the hex view, with the selected bytes painted under its text.
/// </summary>
/// <remarks>
/// <para>
/// A text block that knows it is a hex row: its text says which bytes it shows — a row starts with its own offset —
/// and <see cref="HexFormatter"/> says where each of them stands. Told the view's <see cref="Selection"/>, it paints
/// the part of it on this row twice: in the selection's own column in <see cref="SelectionBrush"/>, and in the other
/// column at half its opacity, so the same bytes read as one selection in both.
/// </para>
/// <para>
/// A band runs unbroken from its first byte to its last, over the spaces between hex pairs, as an editor's
/// selection runs over the spaces between words.
/// </para>
/// </remarks>
public sealed class HexRowBlock : TextBlock
{
    /// <summary>
    /// Defines the <see cref="Selection"/> property.
    /// </summary>
    public static readonly StyledProperty<HexSelection?> SelectionProperty =
        AvaloniaProperty.Register<HexRowBlock, HexSelection?>(nameof(Selection));

    /// <summary>
    /// Defines the <see cref="OffsetDigits"/> property.
    /// </summary>
    public static readonly StyledProperty<int> OffsetDigitsProperty =
        AvaloniaProperty.Register<HexRowBlock, int>(nameof(OffsetDigits), 8);

    /// <summary>
    /// Defines the <see cref="SelectionBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<HexRowBlock, IBrush?>(nameof(SelectionBrush));

    // How strongly the other column shows the selection: the same colour, a step back.
    private const double MirrorOpacity = 0.5;

    static HexRowBlock()
        => AffectsRender<HexRowBlock>(SelectionProperty, OffsetDigitsProperty, SelectionBrushProperty);

    /// <summary>
    /// Gets or sets the bytes selected in the view, on this row or not.
    /// </summary>
    public HexSelection? Selection
    {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    /// <summary>
    /// Gets or sets how many hex digits the row's offset is written with: 8, or 16 beyond 4 GB.
    /// </summary>
    public int OffsetDigits
    {
        get => GetValue(OffsetDigitsProperty);
        set => SetValue(OffsetDigitsProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush the selection is painted with.
    /// </summary>
    public IBrush? SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <summary>
    /// Gets the offset of the row's first byte, read from its text; -1 when the text is not a row.
    /// </summary>
    internal long RowOffset
        => (Text is { } text)
           && (text.Length >= OffsetDigits)
           && long.TryParse(text.AsSpan(0, OffsetDigits), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long offset)
            ? offset
            : -1;

    /// <summary>
    /// Gets how many bytes the row shows: sixteen, or fewer on a short last row.
    /// </summary>
    internal int ByteCount
        => Text is { } text
            ? Math.Clamp(text.Length - HexFormatter.TextCharIndex(0, OffsetDigits), 0, HexFormatter.BytesPerRow)
            : 0;

    /// <summary>
    /// Gets which of the row's bytes are selected, counted from its first; <see langword="null"/> for none.
    /// </summary>
    internal (int First, int Last)? SelectedBytes
    {
        get
        {
            long offset = RowOffset;
            int count = ByteCount;

            if ((Selection is not { } selection) || (offset < 0) || (count == 0)
                || (selection.End < offset) || (selection.Start >= (offset + count)))
            {
                return null;
            }

            return ((int)Math.Max(selection.Start - offset, 0), (int)Math.Min(selection.End - offset, count - 1));
        }
    }

    /// <summary>
    /// Gets where the selected bytes are painted in each column, in the block's coordinates; <see langword="null"/>
    /// when none of the row's bytes are selected.
    /// </summary>
    internal (Rect Hex, Rect Text)? SelectionBands
    {
        get
        {
            if (SelectedBytes is not (int first, int last))
            {
                return null;
            }

            int digits = OffsetDigits;
            int hexStart = HexFormatter.HexCharIndex(first, digits);
            int hexEnd = HexFormatter.HexCharIndex(last, digits) + 2;
            int textStart = HexFormatter.TextCharIndex(first, digits);
            int textEnd = HexFormatter.TextCharIndex(last, digits) + 1;

            return (Band(hexStart, hexEnd), Band(textStart, textEnd));
        }
    }

    /// <inheritdoc />
    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        // Render itself is sealed: the bands go down here, just before the text that is drawn over them.
        if ((SelectionBrush is { } brush) && (SelectionBands is ({ } hex, { } text)) && (Selection is { } selection))
        {
            (Rect primary, Rect mirror) = selection.Column == HexColumn.Hex ? (hex, text) : (text, hex);

            context.FillRectangle(brush, primary);

            using (context.PushOpacity(MirrorOpacity))
            {
                context.FillRectangle(brush, mirror);
            }
        }

        base.RenderTextLayout(context, origin);
    }

    // The characters from start up to end, as one rectangle the height of the row.
    private Rect Band(int start, int end)
    {
        double left = TextLayout.HitTestTextPosition(start).X;
        double right = TextLayout.HitTestTextPosition(end).X;

        return new Rect(Padding.Left + left, 0, right - left, Bounds.Height);
    }
}
