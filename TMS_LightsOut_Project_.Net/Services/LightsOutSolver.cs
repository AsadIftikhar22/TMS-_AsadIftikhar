using LightsOut.Wpf.Models;
using System.Text;

namespace LightsOut.Wpf.Services;

public sealed class LightsOutSolver
{
    private readonly Puzzle _puzzle;
    private readonly int[,] _board;
    private readonly Position[] _positions;
    private readonly List<int[,]> _states = new();
    private readonly HashSet<string> _deadStates = new();

    public LightsOutSolver(Puzzle puzzle)
    {
        _puzzle = puzzle;
        _board = CloneBoard(puzzle.InitialBoard);
        _positions = new Position[puzzle.Pieces.Count];
    }

    public Solution? Solve()
    {
        if (Search(0))
        {
            var states = new List<int[,]>
            {
                CloneBoard(_puzzle.InitialBoard)
            };

            // Rebuild the state sequence from the saved solution.
            int[,] replay = CloneBoard(_puzzle.InitialBoard);

            for (int i = 0; i < _positions.Length; i++)
            {
                Apply(replay, _puzzle.Pieces[i], _positions[i], _puzzle.Depth);
                states.Add(CloneBoard(replay));
            }

            return new Solution(_positions.ToList(), states);
        }

        return null;
    }

    private bool Search(int pieceIndex)
    {
        if (pieceIndex == _puzzle.Pieces.Count)
            return IsSolved();

        string key = CreateKey(pieceIndex);

        if (!_deadStates.Add(key))
            return false;

        Piece piece = _puzzle.Pieces[pieceIndex];

        int maxX = _puzzle.Width - piece.Width;
        int maxY = _puzzle.Height - piece.Height;

        if (maxX < 0 || maxY < 0)
            return false;

        for (int y = 0; y <= maxY; y++)
        {
            for (int x = 0; x <= maxX; x++)
            {
                var position = new Position(x, y);

                Apply(_board, piece, position, _puzzle.Depth);
                _positions[pieceIndex] = position;

                if (Search(pieceIndex + 1))
                    return true;

                Remove(_board, piece, position, _puzzle.Depth);
            }
        }

        return false;
    }

    private bool IsSolved()
    {
        for (int y = 0; y < _puzzle.Height; y++)
        {
            for (int x = 0; x < _puzzle.Width; x++)
            {
                if (_board[y, x] != 0)
                    return false;
            }
        }

        return true;
    }

    private string CreateKey(int pieceIndex)
    {
        var builder = new StringBuilder();

        builder.Append(pieceIndex);
        builder.Append('|');

        for (int y = 0; y < _puzzle.Height; y++)
        {
            for (int x = 0; x < _puzzle.Width; x++)
                builder.Append((char)('0' + _board[y, x]));
        }

        return builder.ToString();
    }

    private static void Apply(
        int[,] board,
        Piece piece,
        Position position,
        int depth)
    {
        for (int py = 0; py < piece.Height; py++)
        {
            for (int px = 0; px < piece.Width; px++)
            {
                if (!piece.Cells[py, px])
                    continue;

                int y = position.Y + py;
                int x = position.X + px;

                board[y, x] = (board[y, x] + 1) % depth;
            }
        }
    }

    private static void Remove(
        int[,] board,
        Piece piece,
        Position position,
        int depth)
    {
        for (int py = 0; py < piece.Height; py++)
        {
            for (int px = 0; px < piece.Width; px++)
            {
                if (!piece.Cells[py, px])
                    continue;

                int y = position.Y + py;
                int x = position.X + px;

                board[y, x] = (board[y, x] - 1 + depth) % depth;
            }
        }
    }

    private static int[,] CloneBoard(int[,] source)
    {
        int height = source.GetLength(0);
        int width = source.GetLength(1);

        var copy = new int[height, width];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                copy[y, x] = source[y, x];

        return copy;
    }
}
