using LightsOut.Wpf.Models;

namespace LightsOut.Wpf.Services;

public static class PuzzleParser
{
    public static Puzzle Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Puzzle input is empty.");

        string[] lines = text
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToArray();

        if (lines.Length < 3)
            throw new ArgumentException("The puzzle must contain exactly three logical lines: depth, board and pieces.");

        if (!int.TryParse(lines[0], out int depth) || depth is < 2 or > 4)
            throw new ArgumentException("Depth must be 2, 3 or 4.");

        string[] boardRows = lines[1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (boardRows.Length == 0)
            throw new ArgumentException("Board is empty.");

        int height = boardRows.Length;
        int width = boardRows[0].Length;

        if (width == 0)
            throw new ArgumentException("Board width is zero.");

        if (boardRows.Any(row => row.Length != width))
            throw new ArgumentException("All board rows must have the same width.");

        int[,] board = new int[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = boardRows[y][x];

                if (!char.IsDigit(c))
                    throw new ArgumentException("Board cells must be digits.");

                int value = c - '0';

                if (value >= depth)
                    throw new ArgumentException($"Board value {value} is invalid for depth {depth}.");

                board[y, x] = value;
            }
        }

        string[] pieceTokens = lines[2]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (pieceTokens.Length == 0)
            throw new ArgumentException("At least one piece is required.");

        List<Piece> pieces = pieceTokens.Select(ParsePiece).ToList();

        return new Puzzle(depth, board, pieces);
    }

    public static string Format(Puzzle puzzle)
    {
        var boardRows = new List<string>();

        for (int y = 0; y < puzzle.Height; y++)
        {
            string row = string.Concat(
                Enumerable.Range(0, puzzle.Width)
                    .Select(x => puzzle.InitialBoard[y, x].ToString()));

            boardRows.Add(row);
        }

        var pieceStrings = puzzle.Pieces.Select(piece =>
        {
            var rows = new List<string>();

            for (int y = 0; y < piece.Height; y++)
            {
                string row = string.Concat(
                    Enumerable.Range(0, piece.Width)
                        .Select(x => piece.Cells[y, x] ? 'X' : '.'));

                rows.Add(row);
            }

            return string.Join(",", rows);
        });

        return $"{puzzle.Depth}{Environment.NewLine}" +
               $"{string.Join(",", boardRows)}{Environment.NewLine}" +
               $"{string.Join(" ", pieceStrings)}";
    }

    private static Piece ParsePiece(string input)
    {
        string[] rows = input.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (rows.Length == 0)
            throw new ArgumentException("Invalid piece.");

        int height = rows.Length;
        int width = rows.Max(row => row.Length);

        if (width == 0)
            throw new ArgumentException("Invalid piece width.");

        bool[,] cells = new bool[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                cells[y, x] = rows[y][x] switch
                {
                    'X' => true,
                    '.' => false,
                    _ => throw new ArgumentException(
                        $"Invalid piece character '{rows[y][x]}'.")
                };
            }
        }

        return new Piece(cells);
    }
}
