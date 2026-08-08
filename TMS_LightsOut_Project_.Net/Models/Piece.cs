namespace LightsOut.Wpf.Models;

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
