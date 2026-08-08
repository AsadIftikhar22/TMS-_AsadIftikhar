namespace LightsOut.Wpf.Models;

public readonly record struct Position(
    int X,
    int Y);

public sealed class Piece
{
    public Piece(
        bool[,] cells)
    {
        Cells =
            cells ??
            throw new ArgumentNullException(
                nameof(cells));

        Height =
            cells.GetLength(0);

        Width =
            cells.GetLength(1);

        if (Height <= 0 ||
            Width <= 0)
        {
            throw new ArgumentException(
                "Piece must contain at least one cell.");
        }
    }

    public bool[,] Cells { get; }

    public int Height { get; }

    public int Width { get; }
}

public sealed class Solution
{
    public Solution(
        IReadOnlyList<Position> positions,
        IReadOnlyList<int[,]> states)
    {
        Positions =
            positions ??
            throw new ArgumentNullException(
                nameof(positions));

        States =
            states ??
            throw new ArgumentNullException(
                nameof(states));
    }

    public IReadOnlyList<Position> Positions { get; }

    public IReadOnlyList<int[,]> States { get; }
}

public sealed class Puzzle
{
    public Puzzle(
        int depth,
        int[,] initialBoard,
        IReadOnlyList<Piece> pieces)
    {
        InitialBoard =
            initialBoard ??
            throw new ArgumentNullException(
                nameof(initialBoard));

        Pieces =
            pieces ??
            throw new ArgumentNullException(
                nameof(pieces));

        Depth =
            depth;

        Height =
            initialBoard.GetLength(0);

        Width =
            initialBoard.GetLength(1);

        if (Height <= 0 ||
            Width <= 0)
        {
            throw new ArgumentException(
                "Board must have at least one cell.");
        }

        if (Height * Width > 100)
        {
            throw new ArgumentException(
                "Maximum board size is 100 cells.");
        }

        if (Depth < 2 ||
            Depth > 5)
        {
            throw new ArgumentException(
                "Depth must be between 2 and 5.");
        }
    }

    public int Depth { get; }

    public int[,] InitialBoard { get; }

    public IReadOnlyList<Piece> Pieces { get; }

    public int Height { get; }

    public int Width { get; }
}

public sealed class PuzzleRecord
{
    public PuzzleRecord(
        int number,
        string input,
        Puzzle puzzle)
    {
        Number = number;
        Input = input;
        Puzzle = puzzle;
    }

    public int Number { get; }

    public string Input { get; }

    public Puzzle Puzzle { get; }

    public Solution? Solution { get; set; }

    public string Status { get; set; } = "Waiting";

    public string Diagnostic { get; set; } = string.Empty;
}