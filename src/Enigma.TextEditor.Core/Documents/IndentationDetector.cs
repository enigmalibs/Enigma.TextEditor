using System;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// Works out how a file is indented from the file itself, so that a tab typed into it matches what is
/// already there.
/// </summary>
/// <remarks>
/// <para>
/// Two questions. <b>Tabs or spaces</b> is a vote between the lines that start with a tab and the lines
/// that start with a space. <b>How many spaces</b> is read from how far each line steps in from the
/// non-blank line before it: a file indented by two steps in by two, over and over, and a step of
/// eight is two levels of four. Steps of one and three are mostly alignment — a block comment's
/// leading <c>*</c>, a continued argument list — and are not counted.
/// </para>
/// <para>
/// Only the first lines are read: indentation is settled long before a file ends, and a heuristic that
/// read a 30 MB log to the end would cost more than it is worth.
/// </para>
/// </remarks>
public static class IndentationDetector
{
    /// <summary>
    /// How many lines are read at most.
    /// </summary>
    public const int MaximumLines = 10_000;

    /// <summary>
    /// Detects a text's indentation.
    /// </summary>
    /// <param name="text">The text, with any line breaks.</param>
    /// <param name="fallback">
    /// What to answer when the text does not say — no indented line, or no step of two or four. Its
    /// size is also the tab size of a tab-indented file.
    /// </param>
    /// <returns>The indentation.</returns>
    public static Indentation Detect(string text, Indentation fallback)
    {
        ArgumentNullException.ThrowIfNull(text);

        int tabLines = 0;
        int spaceLines = 0;
        int stepsOfTwo = 0;
        int stepsOfFour = 0;
        int previousIndent = 0;
        int lines = 0;

        foreach (ReadOnlySpan<char> line in text.AsSpan().EnumerateLines())
        {
            if (++lines > MaximumLines)
            {
                break;
            }

            if (line.IsWhiteSpace())
            {
                continue;
            }

            if (line[0] == '\t')
            {
                tabLines++;
                continue;
            }

            int indent = 0;

            while ((indent < line.Length) && (line[indent] == ' '))
            {
                indent++;
            }

            if (indent > 0)
            {
                spaceLines++;
            }

            // A tab after the spaces makes the width meaningless; the line still voted for spaces.
            if ((indent < line.Length) && (line[indent] == '\t'))
            {
                continue;
            }

            switch (indent - previousIndent)
            {
                case 2:
                    stepsOfTwo++;
                    break;

                case 4:
                case 8:
                    stepsOfFour++;
                    break;

                default:
                    break;
            }

            previousIndent = indent;
        }

        if (tabLines > spaceLines)
        {
            return Indentation.Tabs(fallback.Size);
        }

        if (spaceLines == 0)
        {
            return fallback;
        }

        if ((stepsOfTwo == 0) && (stepsOfFour == 0))
        {
            return Indentation.Spaces(fallback.Size);
        }

        return Indentation.Spaces(stepsOfTwo > stepsOfFour ? 2 : 4);
    }
}
