using System;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// Bytes selected in the hex viewer: a run from the byte the selection started on to the byte it reaches, made in
/// one column.
/// </summary>
/// <remarks>
/// <para>
/// Never empty: the byte it started on is always in it, so <see cref="End"/> is inclusive and a selection of one byte
/// starts and ends on it. The anchor stays where the selection started while the active end follows the pointer,
/// either side of it.
/// </para>
/// <para>
/// The column is the one the selection was made in. The other column shows the same bytes, as their mirror.
/// </para>
/// </remarks>
public readonly record struct HexSelection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HexSelection"/> struct.
    /// </summary>
    /// <param name="anchor">The offset of the byte the selection started on.</param>
    /// <param name="active">The offset of the byte it reaches.</param>
    /// <param name="column">The column it is made in.</param>
    public HexSelection(long anchor, long active, HexColumn column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(anchor);
        ArgumentOutOfRangeException.ThrowIfNegative(active);

        if (!Enum.IsDefined(column))
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Not a column of a hex row.");
        }

        Anchor = anchor;
        Active = active;
        Column = column;
    }

    /// <summary>
    /// Gets the offset of the byte the selection started on.
    /// </summary>
    public long Anchor { get; }

    /// <summary>
    /// Gets the offset of the byte the selection reaches.
    /// </summary>
    public long Active { get; }

    /// <summary>
    /// Gets the column the selection is made in.
    /// </summary>
    public HexColumn Column { get; }

    /// <summary>
    /// Gets the offset of the first byte selected.
    /// </summary>
    public long Start => Math.Min(Anchor, Active);

    /// <summary>
    /// Gets the offset of the last byte selected.
    /// </summary>
    public long End => Math.Max(Anchor, Active);

    /// <summary>
    /// Gets how many bytes are selected; at least one.
    /// </summary>
    public long Length => End - Start + 1;

    /// <summary>
    /// Selects one byte.
    /// </summary>
    /// <param name="offset">The byte.</param>
    /// <param name="column">The column it is selected in.</param>
    /// <returns>The selection.</returns>
    public static HexSelection At(long offset, HexColumn column) => new(offset, offset, column);

    /// <summary>
    /// Moves the active end, keeping the anchor and the column.
    /// </summary>
    /// <param name="offset">The byte the selection now reaches.</param>
    /// <returns>The selection.</returns>
    public HexSelection ExtendedTo(long offset) => new(Anchor, offset, Column);

    /// <summary>
    /// Tells whether a byte is selected.
    /// </summary>
    /// <param name="offset">The byte.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    public bool Contains(long offset) => (offset >= Start) && (offset <= End);
}
