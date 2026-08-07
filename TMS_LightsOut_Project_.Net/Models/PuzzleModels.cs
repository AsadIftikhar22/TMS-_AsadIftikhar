namespace LightsOut.Wpf.Models;

public sealed class Piece
{
    public bool[,] Cells { get; }

    public int Height => Cells.GetLength(0);
    public int Width => Cells.GetLength(1);

    public Piece(bool[,] cells)
    {
        Cells = cells;
    }
}

public readonly record struct Position(int X, int Y);

public sealed class Puzzle
{
    public int Depth { get; }
    public int[,] InitialBoard { get; }
    public IReadOnlyList<Piece> Pieces { get; }

    public int Height => InitialBoard.GetLength(0);
    public int Width => InitialBoard.GetLength(1);

    public Puzzle(int depth, int[,] initialBoard, IReadOnlyList<Piece> pieces)
    {
        Depth = depth;
        InitialBoard = initialBoard;
        Pieces = pieces;
    }
}

public sealed class Solution
{
    public IReadOnlyList<Position> Positions { get; }
    public IReadOnlyList<int[,]> States { get; }

    public Solution(
        IReadOnlyList<Position> positions,
        IReadOnlyList<int[,]> states)
    {
        Positions = positions;
        States = states;
    }
}
