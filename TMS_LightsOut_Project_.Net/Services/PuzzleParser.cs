using LightsOut.Wpf.Models;

namespace LightsOut.Wpf.Services;

public static class PuzzleParser
{
    public static Puzzle Parse(string input)
    {
        var puzzles = ParseMany(input);

        if (puzzles.Count == 0)
        {
            throw new FormatException(
                "No puzzle samples were found.");
        }

        return puzzles[0];
    }

    public static IReadOnlyList<Puzzle> ParseMany(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                "Input is empty.",
                nameof(input));
        }

        string normalized =
            input
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

        string[] blocks =
            normalized
                .Split(
                    "\n\n",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

        var puzzles = new List<Puzzle>();

        /*
         * Some of the supplied sample data contains more than one
         * blank line between records. Normalize each block further.
         */
        foreach (string block in blocks)
        {
            string[] lines =
                block
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

            if (lines.Length < 3)
            {
                continue;
            }

            puzzles.Add(ParseLines(lines));
        }

        /*
         * If blank-line grouping did not work because the source
         * text was copied with unusual whitespace, fall back to
         * sequential parsing.
         */
        if (puzzles.Count == 0)
        {
            puzzles.AddRange(
                ParseSequential(normalized));
        }

        return puzzles;
    }

    private static Puzzle ParseLines(
        string[] lines)
    {
        if (!int.TryParse(
                lines[0],
                out int depth))
        {
            throw new FormatException(
                $"Invalid depth '{lines[0]}'.");
        }

        if (depth < 2 || depth > 5)
        {
            throw new FormatException(
                $"Invalid depth {depth}. " +
                "Depth must be between 2 and 5.");
        }

        string[] boardRows =
            lines[1]
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

        if (boardRows.Length == 0)
        {
            throw new FormatException(
                "Board contains no rows.");
        }

        int height =
            boardRows.Length;

        int width =
            boardRows[0].Length;

        if (width == 0)
        {
            throw new FormatException(
                "Board width is zero.");
        }

        if (height * width > 100)
        {
            throw new FormatException(
                $"Board contains {height * width} cells. " +
                "Maximum supported size is 100 cells.");
        }

        var board =
            new int[height, width];

        for (int y = 0; y < height; y++)
        {
            string row =
                boardRows[y];

            if (row.Length != width)
            {
                throw new FormatException(
                    $"Board row {y + 1} has " +
                    $"{row.Length} characters. " +
                    $"Expected {width}.");
            }

            for (int x = 0; x < width; x++)
            {
                char c = row[x];

                if (c < '0' ||
                    c > '0' + depth - 1)
                {
                    throw new FormatException(
                        $"Invalid board value '{c}' " +
                        $"at ({x},{y}).");
                }

                board[y, x] =
                    c - '0';
            }
        }

        /*
         * Everything after the board is a piece.
         *
         * Pieces are whitespace-separated.
         * A piece itself contains comma-separated rows.
         */
        string pieceText =
            string.Join(
                " ",
                lines.Skip(2));

        string[] pieceStrings =
            pieceText
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

        if (pieceStrings.Length == 0)
        {
            throw new FormatException(
                "No pieces were found.");
        }

        var pieces =
            new List<Piece>();

        for (int i = 0;
             i < pieceStrings.Length;
             i++)
        {
            try
            {
                pieces.Add(
                    ParsePiece(
                        pieceStrings[i]));
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    $"Invalid piece {i + 1}.\n\n" +
                    ex.Message,
                    ex);
            }
        }

        return new Puzzle(
            depth,
            board,
            pieces);
    }

    private static IReadOnlyList<Puzzle> ParseSequential(
        string input)
    {
        string[] lines =
            input
                .Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

        var result =
            new List<Puzzle>();

        int index = 0;

        while (index < lines.Length)
        {
            if (!int.TryParse(
                    lines[index],
                    out int depth))
            {
                index++;
                continue;
            }

            if (index + 2 >= lines.Length)
            {
                break;
            }

            /*
             * The structure of the supplied records is:
             *
             * depth
             * board
             * pieces
             *
             * The next numeric-only line begins the next record.
             */
            int start =
                index;

            index += 2;

            while (index < lines.Length)
            {
                if (int.TryParse(
                        lines[index],
                        out _))
                {
                    break;
                }

                index++;
            }

            string[] block =
                lines
                    .Skip(start)
                    .Take(index - start)
                    .ToArray();

            if (block.Length >= 3)
            {
                result.Add(
                    ParseLines(block));
            }
        }

        return result;
    }

    private static Piece ParsePiece(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new FormatException(
                "Piece is empty.");
        }

        string[] rows =
            text.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (rows.Length == 0)
        {
            throw new FormatException(
                "Piece contains no rows.");
        }

        int height =
            rows.Length;

        int width =
            rows[0].Length;

        if (width == 0)
        {
            throw new FormatException(
                "Piece width is zero.");
        }

        var cells =
            new bool[height, width];

        bool hasActiveCell =
            false;

        for (int y = 0;
             y < height;
             y++)
        {
            if (rows[y].Length != width)
            {
                throw new FormatException(
                    $"Piece row {y + 1} has " +
                    $"{rows[y].Length} characters. " +
                    $"Expected {width}.");
            }

            for (int x = 0;
                 x < width;
                 x++)
            {
                char c =
                    rows[y][x];

                if (c == 'X' ||
                    c == 'x')
                {
                    cells[y, x] = true;
                    hasActiveCell = true;
                }
                else if (c == '.')
                {
                    cells[y, x] = false;
                }
                else
                {
                    throw new FormatException(
                        $"Invalid character '{c}' " +
                        $"at ({x},{y}). " +
                        "Only X and . are allowed.");
                }
            }
        }

        if (!hasActiveCell)
        {
            throw new FormatException(
                "Piece contains no X cells.");
        }

        return new Piece(cells);
    }
}