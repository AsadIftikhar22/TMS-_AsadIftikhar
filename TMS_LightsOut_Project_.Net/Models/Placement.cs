namespace LightsOut.Wpf.Models;

public sealed class Placement
{
    public int PieceId { get; }

    public Position Position { get; }

    public IReadOnlyList<Position> Cells { get; }

    public Placement(
        int pieceId,
        Position position,
        IEnumerable<Position> cells)
    {
        PieceId = pieceId;
        Position = position;
        Cells = cells.ToList();
    }

    public override string ToString()
    {
        return $"P{PieceId}=({Position.X},{Position.Y})";
    }
}