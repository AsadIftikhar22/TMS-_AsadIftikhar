namespace LightsOut.Wpf.Models;

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
