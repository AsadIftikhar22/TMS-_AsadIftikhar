using LightsOut.Wpf.Models;

namespace LightsOut.Wpf.Services;

// Reads the text format used by the Lights Out samples and turns it into Puzzle objects.
// It supports both normally separated samples and input where blank-line formatting was lost.
public static class PuzzleParser
{
    // Parses the first puzzle from the input.
    // This is useful when the caller expects exactly one puzzle.
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

    // Parses every puzzle sample found in the input.
    // The parser first tries blank-line-separated records, then falls back to sequential parsing.
    public static IReadOnlyList<Puzzle> ParseMany(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                "Input is empty.",
                nameof(input));
        }

        // Normalize Windows and old-style line endings so the rest of the parser
        // can work with one consistent newline format.
        string normalized =
            input
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

        // Most sample files separate puzzles with a blank line.
        // Split those blocks first so each block can be parsed independently.
        string[] blocks =
            normalized
                .Split(
                    "\n\n",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

        var puzzles = new List<Puzzle>();

        /*
         * Some of the supplied sample data contains more than one
         * blank line between records. Normalize each block further
         * by removing empty lines and surrounding whitespace.
         */
        foreach (string block in blocks)
        {
            string[] lines =
                block
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

            // A valid puzzle needs at least a depth, a board, and one piece line.
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
         *
         * This makes the parser more tolerant of text copied from
         * websites, documents, terminals, or other applications.
         */
        if (puzzles.Count == 0)
        {
            puzzles.AddRange(
                ParseSequential(normalized));
        }

        return puzzles;
    }

    // Parses one complete puzzle after its lines have already been separated.
    // Expected structure:
    //   line 1 = depth
    //   line 2 = board
    //   remaining lines = pieces
    private static Puzzle ParseLines(
        string[] lines)
    {
        // The first line tells us how many states each board cell can have.
        if (!int.TryParse(
                lines[0],
                out int depth))
        {
            throw new FormatException(
                $"Invalid depth '{lines[0]}'.");
        }

        // The solver only supports cell values from 0 through depth - 1,
        // with depth restricted to the supported range.
        if (depth < 2 || depth > 5)
        {
            throw new FormatException(
                $"Invalid depth {depth}. " +
                "Depth must be between 2 and 5.");
        }

        // The board is stored as comma-separated rows such as:
        // 3302012,3221112,3121312
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

        // Every row must have the same width. The first row gives us
        // the width that all following rows are expected to use.
        int width =
            boardRows[0].Length;

        if (width == 0)
        {
            throw new FormatException(
                "Board width is zero.");
        }

        // Keep the input within the same maximum size supported by the solver.
        if (height * width > 100)
        {
            throw new FormatException(
                $"Board contains {height * width} cells. " +
                "Maximum supported size is 100 cells.");
        }

        var board =
            new int[height, width];

        // Convert each character in the text board into an integer cell value.
        for (int y = 0; y < height; y++)
        {
            string row =
                boardRows[y];

            // A rectangular board is required because the solver uses
            // a two-dimensional array with one fixed width.
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

                // Only values from 0 to depth - 1 are legal board values.
                if (c < '0' ||
                    c > '0' + depth - 1)
                {
                    throw new FormatException(
                        $"Invalid board value '{c}' " +
                        $"at ({x},{y}).");
                }

                // Convert the numeric character, for example '3', into
                // the integer value 3 that the solver works with.
                board[y, x] =
                    c - '0';
            }
        }

        /*
         * Everything after the board is a piece.
         *
         * Pieces are whitespace-separated.
         * A piece itself contains comma-separated rows.
         *
         * Joining the remaining lines first also lets us handle
         * piece data that was wrapped onto multiple text lines.
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

        // Parse every piece independently so an invalid piece can be
        // reported with its piece number.
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

        // At this point the text has been validated and converted into
        // the model object used by the rest of the application.
        return new Puzzle(
            depth,
            board,
            pieces);
    }

    // Fallback parser used when normal blank-line grouping cannot identify
    // separate samples. It treats a numeric-only line as the beginning
    // of a new puzzle record.
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
            // Search for the next line that looks like a puzzle depth.
            if (!int.TryParse(
                    lines[index],
                    out int depth))
            {
                index++;
                continue;
            }

            // We need at least the depth, board, and pieces section.
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

            // Skip the depth and board lines first.
            index += 2;

            // Continue until the next numeric-only line, which marks
            // the beginning of the next puzzle.
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

    // Converts one piece string such as ".X,XX" into a Piece object.
    // X means the piece occupies that cell; . means the cell is empty.
    private static Piece ParsePiece(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new FormatException(
                "Piece is empty.");
        }

        // A piece uses commas to separate its rows.
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

        // Like the board, a piece must be rectangular.
        int width =
            rows[0].Length;

        if (width == 0)
        {
            throw new FormatException(
                "Piece width is zero.");
        }

        var cells =
            new bool[height, width];

        // Used to make sure a piece is not just an empty rectangle of dots.
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

                // X means this cell is part of the piece.
                if (c == 'X' ||
                    c == 'x')
                {
                    cells[y, x] = true;
                    hasActiveCell = true;
                }
                // A dot means this position is empty within the piece shape.
                else if (c == '.')
                {
                    cells[y, x] = false;
                }
                else
                {
                    // Anything other than X/x/. is invalid input.
                    throw new FormatException(
                        $"Invalid character '{c}' " +
                        $"at ({x},{y}). " +
                        "Only X and . are allowed.");
                }
            }
        }

        // An all-dot piece would never affect the board and therefore
        // cannot represent a meaningful puzzle piece.
        if (!hasActiveCell)
        {
            throw new FormatException(
                "Piece contains no X cells.");
        }

        // Convert the parsed boolean grid into the Piece model used by the solver.
        return new Piece(cells);
    }
}
