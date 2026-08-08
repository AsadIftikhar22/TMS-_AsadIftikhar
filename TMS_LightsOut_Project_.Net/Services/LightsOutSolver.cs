using Google.OrTools.Sat;
using LightsOut.Wpf.Models;
using System.Diagnostics;

namespace LightsOut.Wpf.Services;

public sealed class LightsOutSolver
{
    private readonly Puzzle _puzzle;

    private readonly int _height;
    private readonly int _width;
    private readonly int _depth;
    private readonly int _pieceCount;
    private readonly int _cellCount;

    private readonly List<Placement>[] _placements;

    private readonly int _timeLimitSeconds;

    private readonly Stopwatch _stopwatch = new();

    private string _diagnostic = string.Empty;

    private long _branches;
    private long _conflicts;

    private const int DefaultTimeLimitSeconds = 90;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public LightsOutSolver(Puzzle puzzle)
        : this(puzzle, DefaultTimeLimitSeconds)
    {
    }

    public LightsOutSolver(
        Puzzle puzzle,
        int timeLimitSeconds)
    {
        _puzzle =
            puzzle ??
            throw new ArgumentNullException(nameof(puzzle));

        if (timeLimitSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeLimitSeconds));
        }

        _timeLimitSeconds =
            timeLimitSeconds;

        _height =
            puzzle.Height;

        _width =
            puzzle.Width;

        _depth =
            puzzle.Depth;

        _pieceCount =
            puzzle.Pieces.Count;

        _cellCount =
            checked(_height * _width);

        if (_height <= 0 || _width <= 0)
        {
            throw new ArgumentException(
                "Invalid board dimensions.");
        }

        if (_cellCount > 100)
        {
            throw new ArgumentException(
                "Maximum supported board size is 100 cells.");
        }

        if (_depth < 2 || _depth > 5)
        {
            throw new ArgumentException(
                "Depth must be between 2 and 5.");
        }

        if (_pieceCount == 0)
        {
            throw new ArgumentException(
                "Puzzle contains no pieces.");
        }

        _placements =
            BuildPlacements();
    }

    // =========================================================
    // DIAGNOSTIC
    // =========================================================

    public string GetDiagnostic()
    {
        if (!string.IsNullOrWhiteSpace(_diagnostic))
        {
            return _diagnostic;
        }

        return
            $"No solution found.\n\n" +
            $"Board: {_height} × {_width}\n" +
            $"Cells: {_cellCount}\n" +
            $"Depth: {_depth}\n" +
            $"Pieces: {_pieceCount}\n" +
            $"Branches: {_branches:N0}\n" +
            $"Conflicts: {_conflicts:N0}\n" +
            $"Elapsed: {_stopwatch.Elapsed.TotalSeconds:F2}s";
    }

    // =========================================================
    // SOLVE
    // =========================================================

    public Solution? Solve()
    {
        _stopwatch.Restart();

        _diagnostic = string.Empty;
        _branches = 0;
        _conflicts = 0;

        try
        {
            CpModel model = new CpModel();

            /*
             * -------------------------------------------------
             * PLACEMENT VARIABLES
             * -------------------------------------------------
             *
             * variables[piece][placement]
             *
             * exactly one placement must be selected
             * for every piece.
             */

            var variables =
                new BoolVar[_pieceCount][];

            for (int piece = 0;
                 piece < _pieceCount;
                 piece++)
            {
                int count =
                    _placements[piece].Count;

                variables[piece] =
                    new BoolVar[count];

                for (int placement = 0;
                     placement < count;
                     placement++)
                {
                    variables[piece][placement] =
                        model.NewBoolVar(
                            $"P_{piece}_{placement}");
                }

                /*
                 * IMPORTANT:
                 *
                 * Do NOT use AddEquality().
                 *
                 * This works with the newer OR-Tools C#
                 * API:
                 */

                model.AddExactlyOne(
                    variables[piece]);
            }

            /*
             * -------------------------------------------------
             * CELL CONSTRAINTS
             * -------------------------------------------------
             *
             * For every board cell:
             *
             * initial + number_of_hits = 0 (mod depth)
             *
             * Therefore:
             *
             * hits = required + depth * k
             *
             * where:
             *
             * required = (depth - initial) % depth
             */

            for (int cell = 0;
                 cell < _cellCount;
                 cell++)
            {
                var hittingVariables =
                    new List<BoolVar>();

                for (int piece = 0;
                     piece < _pieceCount;
                     piece++)
                {
                    for (int placement = 0;
                         placement < _placements[piece].Count;
                         placement++)
                    {
                        if (PlacementHitsCell(
                                _placements[piece][placement],
                                cell))
                        {
                            hittingVariables.Add(
                                variables[piece][placement]);
                        }
                    }
                }

                int initialValue =
                    GetInitialCellValue(cell);

                int required =
                    (_depth - initialValue) %
                    _depth;

                /*
                 * No placement hits this cell.
                 */

                if (hittingVariables.Count == 0)
                {
                    if (required != 0)
                    {
                        /*
                         * Impossible puzzle.
                         */

                        _stopwatch.Stop();

                        _diagnostic =
                            $"No solution exists.\n\n" +
                            $"Cell {cell} requires " +
                            $"{required} hit(s), but no piece " +
                            $"can hit it.";

                        return null;
                    }

                    continue;
                }

                /*
                 * Hit count.
                 */

                int maximumHits =
                    Math.Min(
                        _pieceCount,
                        hittingVariables.Count);

                IntVar hitCount =
                    model.NewIntVar(
                        0,
                        maximumHits,
                        $"H_{cell}");

                /*
                 * -------------------------------------------------
                 * IMPORTANT
                 * -------------------------------------------------
                 *
                 * Instead of:
                 *
                 * model.AddEquality(...)
                 *
                 * use:
                 *
                 * model.Add(
                 *     hitCount ==
                 *     LinearExpr.Sum(...));
                 */

                model.Add(
                    hitCount ==
                    LinearExpr.Sum(
                        hittingVariables));

                /*
                 * k can never need to be greater than
                 * maximumHits / depth.
                 */

                int maxK =
                    maximumHits / _depth;

                IntVar k =
                    model.NewIntVar(
                        0,
                        maxK,
                        $"K_{cell}");

                model.Add(
                    hitCount ==
                    LinearExpr.Constant(required) +
                    _depth * k);
            }

            /*
             * -------------------------------------------------
             * SOLVER
             * -------------------------------------------------
             */

            CpSolver solver =
                new CpSolver();

            solver.StringParameters =
                string.Join(
                    " ",
                    $"max_time_in_seconds:{_timeLimitSeconds}",
                    "num_search_workers:8",
                    "log_search_progress:false",
                    "cp_model_presolve:true",
                    "cp_model_probing_level:2",
                    "symmetry_level:2");

            CpSolverStatus status =
                solver.Solve(model);

            _branches =
                solver.NumBranches();

            _conflicts =
                solver.NumConflicts();

            _stopwatch.Stop();

            /*
             * -------------------------------------------------
             * STATUS
             * -------------------------------------------------
             */

            if (status != CpSolverStatus.Optimal &&
                status != CpSolverStatus.Feasible)
            {
                if (status == CpSolverStatus.Unknown)
                {
                    _diagnostic =
                        $"Solver timed out or returned " +
                        $"an unknown result.\n\n" +
                        $"Time: " +
                        $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                        $"Branches: {_branches:N0}\n" +
                        $"Conflicts: {_conflicts:N0}";

                    return null;
                }

                _diagnostic =
                    $"No solution exists.\n\n" +
                    $"OR-Tools status: {status}\n" +
                    $"Time: " +
                    $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                    $"Branches: {_branches:N0}\n" +
                    $"Conflicts: {_conflicts:N0}";

                return null;
            }

            /*
             * -------------------------------------------------
             * EXTRACT POSITIONS
             * -------------------------------------------------
             */

            var positions =
                new Position[_pieceCount];

            for (int piece = 0;
                 piece < _pieceCount;
                 piece++)
            {
                int selected =
                    -1;

                for (int placement = 0;
                     placement < variables[piece].Length;
                     placement++)
                {
                    long value =
                        solver.Value(
                            variables[piece][placement]);

                    if (value != 0)
                    {
                        selected =
                            placement;

                        break;
                    }
                }

                if (selected < 0)
                {
                    throw new InvalidOperationException(
                        $"OR-Tools returned no placement " +
                        $"for piece {piece + 1}.");
                }

                positions[piece] =
                    _placements[piece]
                        [selected]
                        .Position;
            }

            /*
             * -------------------------------------------------
             * BUILD SOLUTION
             * -------------------------------------------------
             */

            Solution solution =
                CreateSolution(
                    positions);

            /*
             * CreateSolution performs a second
             * independent verification.
             */

            _diagnostic =
                $"Solved successfully.\n" +
                $"Time: " +
                $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                $"Branches: {_branches:N0}\n" +
                $"Conflicts: {_conflicts:N0}";

            return solution;
        }
        catch (Exception ex)
        {
            _stopwatch.Stop();

            _diagnostic =
                $"Solver exception.\n\n" +
                $"{ex}";

            return null;
        }
    }

    // =========================================================
    // INITIAL CELL
    // =========================================================

    private int GetInitialCellValue(
        int cell)
    {
        int y =
            cell / _width;

        int x =
            cell % _width;

        return _puzzle.InitialBoard[y, x];
    }

    // =========================================================
    // BUILD PLACEMENTS
    // =========================================================

    private List<Placement>[] BuildPlacements()
    {
        var result =
            new List<Placement>[_pieceCount];

        for (int pieceIndex = 0;
             pieceIndex < _pieceCount;
             pieceIndex++)
        {
            Piece piece =
                _puzzle.Pieces[pieceIndex];

            int maxX =
                _width - piece.Width;

            int maxY =
                _height - piece.Height;

            if (maxX < 0 ||
                maxY < 0)
            {
                throw new InvalidOperationException(
                    $"Piece {pieceIndex + 1} " +
                    $"({piece.Width}×{piece.Height}) " +
                    $"is larger than board " +
                    $"({_width}×{_height}).");
            }

            var placements =
                new List<Placement>();

            int index = 0;

            for (int y = 0;
                 y <= maxY;
                 y++)
            {
                for (int x = 0;
                     x <= maxX;
                     x++)
                {
                    var cells =
                        new List<int>();

                    for (int py = 0;
                         py < piece.Height;
                         py++)
                    {
                        for (int px = 0;
                             px < piece.Width;
                             px++)
                        {
                            if (!piece.Cells[py, px])
                            {
                                continue;
                            }

                            int boardX =
                                x + px;

                            int boardY =
                                y + py;

                            if (boardX < 0 ||
                                boardX >= _width ||
                                boardY < 0 ||
                                boardY >= _height)
                            {
                                continue;
                            }

                            cells.Add(
                                boardY * _width +
                                boardX);
                        }
                    }

                    if (cells.Count == 0)
                    {
                        continue;
                    }

                    cells.Sort();

                    placements.Add(
                        new Placement(
                            index++,
                            new Position(x, y),
                            cells.ToArray()));
                }
            }

            if (placements.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Piece {pieceIndex + 1} " +
                    "has no valid placements.");
            }

            result[pieceIndex] =
                placements;
        }

        return result;
    }

    // =========================================================
    // HIT TEST
    // =========================================================

    private static bool PlacementHitsCell(
        Placement placement,
        int cell)
    {
        /*
         * Cells are sorted.
         *
         * Binary search is much faster than
         * scanning the complete array.
         */

        return Array.BinarySearch(
            placement.Cells,
            cell) >= 0;
    }

    // =========================================================
    // CREATE SOLUTION
    // =========================================================

    private Solution CreateSolution(
        Position[] positions)
    {
        if (positions.Length != _pieceCount)
        {
            throw new InvalidOperationException(
                "Incorrect number of solution positions.");
        }

        var states =
            new List<int[,]>(
                _pieceCount + 1);

        int[,] board =
            CloneBoard(
                _puzzle.InitialBoard);

        states.Add(
            CloneBoard(board));

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            Position position =
                positions[piece];

            ApplyPieceToBoard(
                board,
                _puzzle.Pieces[piece],
                position);

            states.Add(
                CloneBoard(board));
        }

        /*
         * Final independent verification.
         */

        VerifyBoardSolved(board);

        return new Solution(
            positions,
            states);
    }

    // =========================================================
    // APPLY PIECE
    // =========================================================

    private void ApplyPieceToBoard(
        int[,] board,
        Piece piece,
        Position position)
    {
        for (int py = 0;
             py < piece.Height;
             py++)
        {
            for (int px = 0;
                 px < piece.Width;
                 px++)
            {
                if (!piece.Cells[py, px])
                {
                    continue;
                }

                int x =
                    position.X + px;

                int y =
                    position.Y + py;

                if (x < 0 ||
                    x >= _width ||
                    y < 0 ||
                    y >= _height)
                {
                    throw new InvalidOperationException(
                        $"Piece placed outside board " +
                        $"at ({x},{y}).");
                }

                board[y, x] =
                    (board[y, x] + 1) %
                    _depth;
            }
        }
    }

    // =========================================================
    // VERIFY
    // =========================================================

    private void VerifyBoardSolved(
        int[,] board)
    {
        for (int y = 0;
             y < _height;
             y++)
        {
            for (int x = 0;
                 x < _width;
                 x++)
            {
                if (board[y, x] != 0)
                {
                    throw new InvalidOperationException(
                        $"Final verification failed.\n\n" +
                        $"Cell ({x},{y}) = " +
                        $"{board[y, x]}.");
                }
            }
        }
    }

    // =========================================================
    // CLONE
    // =========================================================

    private static int[,] CloneBoard(
        int[,] source)
    {
        int height =
            source.GetLength(0);

        int width =
            source.GetLength(1);

        var result =
            new int[height, width];

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                result[y, x] =
                    source[y, x];
            }
        }

        return result;
    }

    // =========================================================
    // PLACEMENT
    // =========================================================

    private sealed class Placement
    {
        public Placement(
            int index,
            Position position,
            int[] cells)
        {
            Index =
                index;

            Position =
                position;

            Cells =
                cells;
        }

        public int Index { get; }

        public Position Position { get; }

        public int[] Cells { get; }
    }
}