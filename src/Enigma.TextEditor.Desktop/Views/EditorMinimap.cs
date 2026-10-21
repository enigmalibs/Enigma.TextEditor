using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The strip beside the text that says which part of the document is on screen and where the caret
/// is, and scrolls the document to wherever it is pressed.
/// </summary>
/// <remarks>
/// <para>
/// Enigma.GitClient's diff minimap, for an editor: it takes the vertical scrollbar's place, and it is
/// wide enough to aim at with a trackpad, which the scrollbar is not.
/// </para>
/// <para>
/// It knows nothing about editors or scroll viewers: it is told where the viewport starts and ends and
/// where the caret is, as fractions of the whole document, and it raises <see cref="ScrollRequested"/>
/// when the reader points at a part of the document. Whoever owns the scrolling answers that, which
/// keeps this control testable without a window full of text.
/// </para>
/// <para>
/// It draws the caret, not the code: a picture of thousands of lines at two pixels tall costs a great
/// deal and says less than one mark at the line the reader is on.
/// </para>
/// </remarks>
public sealed class EditorMinimap : Control
{
    /// <summary>
    /// Defines the <see cref="ViewportStart"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ViewportStartProperty =
        AvaloniaProperty.Register<EditorMinimap, double>(nameof(ViewportStart));

    /// <summary>
    /// Defines the <see cref="ViewportEnd"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ViewportEndProperty =
        AvaloniaProperty.Register<EditorMinimap, double>(nameof(ViewportEnd), 1);

    /// <summary>
    /// Defines the <see cref="CaretPosition"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CaretPositionProperty =
        AvaloniaProperty.Register<EditorMinimap, double>(nameof(CaretPosition), double.NaN);

    /// <summary>
    /// Defines the <see cref="TrackBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<EditorMinimap, IBrush?>(nameof(TrackBrush));

    /// <summary>
    /// Defines the <see cref="CaretBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<EditorMinimap, IBrush?>(nameof(CaretBrush));

    /// <summary>
    /// Defines the <see cref="ViewportBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ViewportBrushProperty =
        AvaloniaProperty.Register<EditorMinimap, IBrush?>(nameof(ViewportBrush));

    /// <summary>
    /// Defines the <see cref="ViewportBorderBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ViewportBorderBrushProperty =
        AvaloniaProperty.Register<EditorMinimap, IBrush?>(nameof(ViewportBorderBrush));

    /// <summary>
    /// The least a mark may ever be drawn at, whatever the strip's width. One line in a
    /// four-thousand-line file is a fraction of a pixel, and a mark nobody can see is not there.
    /// </summary>
    private const double SmallestMarkHeight = 2;

    /// <summary>
    /// How much of the strip's width is kept clear on each side of the caret mark.
    /// </summary>
    private const double SideInset = 0.1;

    /// <summary>How tall a mark may be at its smallest, as a fraction of the strip's width.</summary>
    private const double MarkHeightOfWidth = 1.0 / 12;

    /// <summary>How thick the window's outline is, as a fraction of the strip's width.</summary>
    private const double OutlineOfWidth = 1.0 / 18;

    static EditorMinimap()
    {
        AffectsRender<EditorMinimap>(
            ViewportStartProperty,
            ViewportEndProperty,
            CaretPositionProperty,
            TrackBrushProperty,
            CaretBrushProperty,
            ViewportBrushProperty,
            ViewportBorderBrushProperty);
    }

    /// <summary>
    /// Raised when the reader points at a part of the document, carrying where the viewport should
    /// start as a fraction of the whole.
    /// </summary>
    public event EventHandler<double>? ScrollRequested;

    /// <summary>Gets or sets where the visible part of the document begins, from 0 to 1.</summary>
    public double ViewportStart
    {
        get => GetValue(ViewportStartProperty);
        set => SetValue(ViewportStartProperty, value);
    }

    /// <summary>Gets or sets where the visible part of the document ends, from 0 to 1.</summary>
    public double ViewportEnd
    {
        get => GetValue(ViewportEndProperty);
        set => SetValue(ViewportEndProperty, value);
    }

    /// <summary>
    /// Gets or sets where the caret's line begins, from 0 to 1, or <see cref="double.NaN"/> for no mark.
    /// </summary>
    public double CaretPosition
    {
        get => GetValue(CaretPositionProperty);
        set => SetValue(CaretPositionProperty, value);
    }

    /// <summary>Gets or sets the brush the strip itself is painted in.</summary>
    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    /// <summary>Gets or sets the brush the caret's mark is drawn in.</summary>
    public IBrush? CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>Gets or sets the brush the window over the visible part is filled with.</summary>
    public IBrush? ViewportBrush
    {
        get => GetValue(ViewportBrushProperty);
        set => SetValue(ViewportBrushProperty, value);
    }

    /// <summary>Gets or sets the brush that window is outlined in.</summary>
    public IBrush? ViewportBorderBrush
    {
        get => GetValue(ViewportBorderBrushProperty);
        set => SetValue(ViewportBorderBrushProperty, value);
    }

    /// <summary>
    /// Works out where the viewport should start for a pointer at a given height.
    /// </summary>
    /// <param name="y">Where the pointer is, in the map's own coordinates.</param>
    /// <param name="height">How tall the map is.</param>
    /// <param name="viewportFraction">How much of the document is on screen, from 0 to 1.</param>
    /// <returns>Where the viewport should start, from 0 to 1.</returns>
    /// <remarks>
    /// The pointer is the middle of the window rather than its top: pointing at a place is asking to
    /// look at it, and a window that started there would put it on the first line, with what comes
    /// before it off screen.
    /// </remarks>
    public static double StartFor(double y, double height, double viewportFraction)
    {
        if (height <= 0)
        {
            return 0;
        }

        double visible = Math.Clamp(viewportFraction, 0, 1);
        double centre = Math.Clamp(y / height, 0, 1);

        return Math.Clamp(centre - (visible / 2), 0, Math.Max(0, 1 - visible));
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = new(Bounds.Size);

        if ((bounds.Width <= 0) || (bounds.Height <= 0))
        {
            return;
        }

        if (TrackBrush is { } track)
        {
            context.FillRectangle(track, bounds);
        }

        DrawCaret(context, bounds);
        DrawViewport(context, bounds);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        e.Pointer.Capture(this);
        RequestScrollTo(e.GetPosition(this).Y);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Only while the press that started the drag is still held, which is what the capture says.
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            RequestScrollTo(e.GetPosition(this).Y);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Asks for the document to be scrolled so the pointer's height is in the middle of the view.
    /// </summary>
    /// <param name="y">Where the pointer is, in the map's own coordinates.</param>
    internal void RequestScrollTo(double y)
        => ScrollRequested?.Invoke(this, StartFor(y, Bounds.Height, ViewportEnd - ViewportStart));

    private double SmallestFor(Rect bounds) => Math.Max(SmallestMarkHeight, bounds.Width * MarkHeightOfWidth);

    private void DrawCaret(DrawingContext context, Rect bounds)
    {
        if (double.IsNaN(CaretPosition) || (CaretBrush is not { } brush))
        {
            return;
        }

        double inset = bounds.Width * SideInset;
        double height = SmallestFor(bounds);

        // A caret on the last line must not be drawn past the bottom by its own minimum height.
        double top = Math.Min(bounds.Height * Math.Clamp(CaretPosition, 0, 1), Math.Max(0, bounds.Height - height));

        context.FillRectangle(brush, new Rect(inset, top, Math.Max(1, bounds.Width - (inset * 2)), height));
    }

    private void DrawViewport(DrawingContext context, Rect bounds)
    {
        double start = Math.Clamp(ViewportStart, 0, 1);
        double end = Math.Clamp(ViewportEnd, start, 1);

        // Nothing to say when the whole document is on screen: a window around everything is noise.
        if ((end - start) >= 1)
        {
            return;
        }

        double height = Math.Max(SmallestFor(bounds), bounds.Height * (end - start));
        double top = Math.Min(bounds.Height * start, Math.Max(0, bounds.Height - height));
        Rect window = new(0, top, bounds.Width, height);

        if (ViewportBrush is { } fill)
        {
            context.FillRectangle(fill, window);
        }

        if (ViewportBorderBrush is { } border)
        {
            // The outline grows with the strip: one pixel around a thirty-six-pixel window is the
            // wash's edge rather than a frame around where the reader is.
            double thickness = Math.Max(1, bounds.Width * OutlineOfWidth);

            context.DrawRectangle(null, new Pen(border, thickness), window.Deflate(thickness / 2));
        }
    }
}
