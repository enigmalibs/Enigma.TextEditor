using System;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// Turns the deltas a pointer reports into whole zoom steps.
/// </summary>
/// <remarks>
/// <para>
/// A mouse wheel turns one notch at a time, but a precision touchpad, a high-resolution wheel or a pinch on a Windows
/// touchpad report fractions of a notch. If each event were rounded, a touchpad would zoom far too fast or never
/// move. So the deltas are added up and a step is taken each time a whole <see cref="Threshold"/> has gathered. What
/// is left over waits for the next event.
/// </para>
/// <para>
/// A change of direction drops the remainder: going back should start from nothing, not first undo what was left of
/// the way forward.
/// </para>
/// </remarks>
internal sealed class ZoomAccumulator
{
    // Far more than the whole font size range: a delta this large asks for an end of it, and is kept within an int.
    private const int MaximumSteps = 1000;

    private const double Tolerance = 1e-9;

    private double _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomAccumulator"/> class.
    /// </summary>
    /// <param name="threshold">How much delta makes one step; more than zero.</param>
    public ZoomAccumulator(double threshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);

        Threshold = threshold;
    }

    /// <summary>
    /// Gets how much delta makes one step.
    /// </summary>
    public double Threshold { get; }

    /// <summary>
    /// Adds a delta and takes the whole steps it completes.
    /// </summary>
    /// <param name="delta">The delta reported: positive zooms in, negative out.</param>
    /// <returns>The steps to zoom by: positive in, negative out, zero while a step is still gathering.</returns>
    public int Add(double delta)
    {
        if (!double.IsFinite(delta) || (delta == 0))
        {
            return 0;
        }

        if (Math.Sign(delta) != Math.Sign(_pending))
        {
            _pending = 0;
        }

        _pending += delta;

        // Truncated towards zero, so the remainder keeps the sign of the way the pointer is going. The tolerance
        // handles fractions that do not add up exactly: ten deltas of 0.1 make 0.9999999999999999, which is one step.
        double ratio = _pending / Threshold;

        if (Math.Abs(ratio) >= MaximumSteps)
        {
            _pending = 0;

            return Math.Sign(ratio) * MaximumSteps;
        }

        int steps = (int)(ratio + (Math.Sign(ratio) * Tolerance));
        _pending -= steps * Threshold;

        return steps;
    }
}
