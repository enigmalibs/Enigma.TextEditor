using System;
using System.Collections;
using System.Collections.Generic;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// The rows of a hex dump, formatted when they are asked for.
/// </summary>
/// <remarks>
/// <para>
/// A list a virtualizing view can index into: it asks for the rows on screen, and only those are read
/// and formatted — 64 rows at a time, the last few pages kept, so scrolling back over what was just
/// shown reads nothing again. A 1 GB file is 67 million rows here and a few kilobytes of memory.
/// </para>
/// <para>
/// Read-only through both list interfaces. The non-generic <see cref="IList"/> is what lets a view
/// index rather than enumerate.
/// </para>
/// </remarks>
public sealed class HexRows : IReadOnlyList<string>, IList
{
    /// <summary>
    /// How many rows are read and formatted together.
    /// </summary>
    public const int RowsPerPage = 64;

    private const int PagesKept = 16;

    private readonly IByteSource _source;
    private readonly int _offsetDigits;
    private readonly Dictionary<long, string[]> _pages = [];
    private readonly LinkedList<long> _recent = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HexRows"/> class.
    /// </summary>
    /// <param name="source">The bytes.</param>
    public HexRows(IByteSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _offsetDigits = HexFormatter.OffsetDigits(source.Length);

        // A list indexes with an int: past two billion rows (32 GB) the end of the file is not shown.
        Count = (int)Math.Min(HexFormatter.RowCount(source.Length), int.MaxValue);
    }

    /// <summary>
    /// Gets how many pages have been read so far — for the tests that check rows are read on demand.
    /// </summary>
    public int PagesRead { get; private set; }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count { get; }

    /// <inheritdoc />
    bool IList.IsFixedSize => true;

    /// <inheritdoc />
    bool IList.IsReadOnly => true;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot => this;

    /// <inheritdoc />
    public string this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return Page(index / RowsPerPage)[index % RowsPerPage];
        }
    }

    /// <inheritdoc />
    object? IList.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// The offset of a row's first byte.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The offset.</returns>
    public static long OffsetOf(int row) => (long)row * HexFormatter.BytesPerRow;

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator()
    {
        for (int index = 0; index < Count; index++)
        {
            yield return this[index];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    bool IList.Contains(object? value) => (value is string row) && (IndexOfRow(row) >= 0);

    /// <inheritdoc />
    int IList.IndexOf(object? value) => value is string row ? IndexOfRow(row) : -1;

    /// <inheritdoc />
    int IList.Add(object? value) => throw new NotSupportedException();

    /// <inheritdoc />
    void IList.Clear() => throw new NotSupportedException();

    /// <inheritdoc />
    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    /// <inheritdoc />
    void IList.Remove(object? value) => throw new NotSupportedException();

    /// <inheritdoc />
    void IList.RemoveAt(int index) => throw new NotSupportedException();

    /// <inheritdoc />
    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);

        for (int row = 0; row < Count; row++)
        {
            array.SetValue(this[row], index + row);
        }
    }

    // A row starts with its own offset, so its text says where it is without a scan of the file.
    private int IndexOfRow(string row)
    {
        if ((row.Length < _offsetDigits)
            || !long.TryParse(row.AsSpan(0, _offsetDigits), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out long offset)
            || ((offset % HexFormatter.BytesPerRow) != 0))
        {
            return -1;
        }

        long index = offset / HexFormatter.BytesPerRow;

        return (index < Count) && string.Equals(this[(int)index], row, StringComparison.Ordinal) ? (int)index : -1;
    }

    private string[] Page(long page)
    {
        if (_pages.TryGetValue(page, out string[]? rows))
        {
            _recent.Remove(page);
            _recent.AddFirst(page);

            return rows;
        }

        int first = (int)(page * RowsPerPage);
        int count = Math.Min(RowsPerPage, Count - first);
        byte[] bytes = new byte[count * HexFormatter.BytesPerRow];
        int read = _source.Read(OffsetOf(first), bytes);

        rows = new string[count];

        for (int index = 0; index < count; index++)
        {
            int start = Math.Min(index * HexFormatter.BytesPerRow, read);
            int length = Math.Min(HexFormatter.BytesPerRow, read - start);
            rows[index] = HexFormatter.FormatRow(OffsetOf(first + index), bytes.AsSpan(start, Math.Max(length, 0)), _offsetDigits);
        }

        _pages[page] = rows;
        _recent.AddFirst(page);
        PagesRead++;

        if (_recent.Count > PagesKept)
        {
            _pages.Remove(_recent.Last!.Value);
            _recent.RemoveLast();
        }

        return rows;
    }
}
