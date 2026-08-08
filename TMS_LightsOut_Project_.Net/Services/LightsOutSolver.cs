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

    private bool _wasTimeout;

    private const int DefaultTimeLimitSeconds = 300;

    //
    // Fallback gets additional time, but does NOT create
    // diagram states.
    //
    private const int FallbackTimeLimitSeconds = 300;

    public LightsOutSolver(Puzzle puzzle)
        : this(
            puzzle,
            DefaultTimeLimitSeconds)
    {
    }

    public LightsOutSolver(
        Puzzle puzzle,
        int timeLimitSeconds)
    {
        _puzzle =
            puzzle ??
            throw new ArgumentNullException(
                nameof(puzzle));

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
            checked(
                _height *
                _width);

        if (_height <= 0 ||
            _width <= 0)
        {
            throw new ArgumentException(
                "Invalid board dimensions.");
        }

        if (_cellCount > 100)
        {
            throw new ArgumentException(
                "Maximum supported board size is 100 cells.");
        }

        if (_depth < 2 ||
            _depth > 5)
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
        if (!string.IsNullOrWhiteSpace(
                _diagnostic))
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
            $"Elapsed: " +
            $"{_stopwatch.Elapsed.TotalSeconds:F2}s";
    }

    // =========================================================
    // WAS TIMEOUT?
    // =========================================================

    public bool WasTimeout =>
        _wasTimeout;

    // =========================================================
    // NORMAL SOLVE
    // =========================================================

    public Solution? Solve()
    {
        _stopwatch.Restart();

        _diagnostic =
            string.Empty;

        _branches = 0;

        _conflicts = 0;

        _wasTimeout = false;

        try
        {
            CpModel model =
                BuildModel(
                    out BoolVar[][] selected);

            CpSolver solver =
                CreateSolver(
                    _timeLimitSeconds);

            CpSolverStatus status =
                solver.Solve(model);

            _branches =
                solver.NumBranches();

            _conflicts =
                solver.NumConflicts();

            _stopwatch.Stop();

            if (status !=
                    CpSolverStatus.Optimal &&
                status !=
                    CpSolverStatus.Feasible)
            {
                if (status ==
                    CpSolverStatus.Unknown)
                {
                    _wasTimeout =
                        true;

                    _diagnostic =
                        $"Normal solver reached its " +
                        $"time limit.\n\n" +
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

            Position[] positions =
                ExtractPositions(
                    solver,
                    selected);

            Solution solution =
                CreateSolution(
                    positions);

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
    // ANSWER-ONLY FALLBACK
    // =========================================================

    public Solution? SolveAnswerOnly()
    {
        Stopwatch fallbackWatch =
            Stopwatch.StartNew();

        try
        {
            CpModel model =
                BuildModel(
                    out BoolVar[][] selected);

            CpSolver solver =
                CreateSolver(
                    FallbackTimeLimitSeconds);

            CpSolverStatus status =
                solver.Solve(model);

            long fallbackBranches =
                solver.NumBranches();

            long fallbackConflicts =
                solver.NumConflicts();

            fallbackWatch.Stop();

            if (status !=
                    CpSolverStatus.Optimal &&
                status !=
                    CpSolverStatus.Feasible)
            {
                _diagnostic =
                    $"Normal search timed out and " +
                    $"the answer-only fallback did not " +
                    $"find a solution.\n\n" +
                    $"Fallback status: {status}\n" +
                    $"Fallback time: " +
                    $"{fallbackWatch.Elapsed.TotalSeconds:F2}s\n" +
                    $"Fallback branches: " +
                    $"{fallbackBranches:N0}\n" +
                    $"Fallback conflicts: " +
                    $"{fallbackConflicts:N0}";

                return null;
            }

            Position[] positions =
                ExtractPositions(
                    solver,
                    selected);

            //
            // IMPORTANT:
            //
            // Do NOT call CreateSolution() here.
            //
            // That method creates all diagram states.
            //
            // We deliberately return an empty States list.
            //

            var solution =
                new Solution(
                    positions,
                    Array.Empty<int[,]>());

            _diagnostic =
                $"Solved using answer-only fallback.\n" +
                $"Normal search reached its time limit.\n\n" +
                $"Fallback time: " +
                $"{fallbackWatch.Elapsed.TotalSeconds:F2}s\n" +
                $"Fallback branches: " +
                $"{fallbackBranches:N0}\n" +
                $"Fallback conflicts: " +
                $"{fallbackConflicts:N0}";

            return solution;
        }
        catch (Exception ex)
        {
            fallbackWatch.Stop();

            _diagnostic =
                $"Fallback solver exception.\n\n" +
                $"{ex}";

            return null;
        }
    }

    // =========================================================
    // BUILD MODEL
    // =========================================================

    private CpModel BuildModel(
        out BoolVar[][] selected)
    {
        CpModel model =
            new CpModel();

        selected =
            new BoolVar[_pieceCount][];

        // =====================================================
        // ONE BOOLEAN PER PLACEMENT
        // =====================================================

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            int count =
                _placements[piece].Count;

            selected[piece] =
                new BoolVar[count];

            for (int placement = 0;
                 placement < count;
                 placement++)
            {
                selected[piece][placement] =
                    model.NewBoolVar(
                        $"P{piece}_Placement{placement}");
            }

            model.AddExactlyOne(
                selected[piece]);
        }

        // =====================================================
        // CELL CONSTRAINTS
        // =====================================================

        for (int cell = 0;
             cell < _cellCount;
             cell++)
        {
            var hits =
                new List<ILiteral>();

            for (int piece = 0;
                 piece < _pieceCount;
                 piece++)
            {
                for (int placement = 0;
                     placement <
                     _placements[piece].Count;
                     placement++)
                {
                    if (Array.BinarySearch(
                            _placements[piece]
                                [placement]
                                .Cells,
                            cell) >= 0)
                    {
                        hits.Add(
                            selected[piece][placement]);
                    }
                }
            }

            int initial =
                GetInitialCellValue(
                    cell);

            if (hits.Count == 0)
            {
                if (initial != 0)
                {
                    model.Add(
                        LinearExpr.Constant(1) ==
                        0);
                }

                continue;
            }

            BoolVar[] hitVars =
                hits
                    .Cast<BoolVar>()
                    .ToArray();

            LinearExpr totalHits =
                LinearExpr.Sum(
                    hitVars);

            int required =
                (_depth - initial) %
                _depth;

            IntVar remainder =
                model.NewIntVar(
                    0,
                    _depth - 1,
                    $"Remainder_{cell}");

            model.AddModuloEquality(
                remainder,
                totalHits,
                _depth);

            model.Add(
                remainder ==
                required);
        }

        // =====================================================
        // SYMMETRY
        // =====================================================

        AddIdenticalPieceSymmetryBreaking(
            model,
            selected);

        return model;
    }

    // =========================================================
    // CREATE SOLVER
    // =========================================================

    private static CpSolver CreateSolver(
        int seconds)
    {
        var solver =
            new CpSolver();

        solver.StringParameters =
            string.Join(
                " ",
                $"max_time_in_seconds:{seconds}",
                "num_search_workers:8",
                "log_search_progress:false",
                "cp_model_presolve:true",
                "cp_model_probing_level:2",
                "symmetry_level:2",
                "linearization_level:2",
                "randomize_search:false");

        return solver;
    }

    // =========================================================
    // EXTRACT POSITIONS
    // =========================================================

    private Position[] ExtractPositions(
        CpSolver solver,
        BoolVar[][] selected)
    {
        var positions =
            new Position[_pieceCount];

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            int selectedPlacement =
                -1;

            for (int placement = 0;
                 placement <
                 selected[piece].Length;
                 placement++)
            {
                if (solver.Value(
                        selected[piece][placement]) >
                    0)
                {
                    selectedPlacement =
                        placement;

                    break;
                }
            }

            if (selectedPlacement < 0)
            {
                throw new InvalidOperationException(
                    $"No placement selected for " +
                    $"piece {piece + 1}.");
            }

            positions[piece] =
                _placements[piece]
                    [selectedPlacement]
                    .Position;
        }

        return positions;
    }

    // =========================================================
    // SYMMETRY BREAKING
    // =========================================================

    private void AddIdenticalPieceSymmetryBreaking(
        CpModel model,
        BoolVar[][] selected)
    {
        for (int first = 0;
             first < _pieceCount;
             first++)
        {
            for (int second = first + 1;
                 second < _pieceCount;
                 second++)
            {
                if (!PiecesAreIdentical(
                        _puzzle.Pieces[first],
                        _puzzle.Pieces[second]))
                {
                    continue;
                }

                int firstCount =
                    selected[first].Length;

                int secondCount =
                    selected[second].Length;

                int max =
                    Math.Min(
                        firstCount,
                        secondCount);

                for (int boundary = 0;
                     boundary < max;
                     boundary++)
                {
                    var secondLower =
                        new List<ILiteral>();

                    for (int j = 0;
                         j < boundary;
                         j++)
                    {
                        secondLower.Add(
                            selected[second][j]);
                    }

                    if (secondLower.Count == 0)
                    {
                        continue;
                    }

                    var firstHigh =
                        new List<ILiteral>();

                    for (int i = boundary;
                         i < firstCount;
                         i++)
                    {
                        firstHigh.Add(
                            selected[first][i]);
                    }

                    if (firstHigh.Count == 0)
                    {
                        continue;
                    }

                    model.AddAtMostOne(
                        firstHigh
                            .Concat(secondLower)
                            .ToArray());
                }
            }
        }
    }

    // =========================================================
    // PIECE COMPARISON
    // =========================================================

    private static bool PiecesAreIdentical(
        Piece first,
        Piece second)
    {
        if (first.Height != second.Height ||
            first.Width != second.Width)
        {
            return false;
        }

        for (int y = 0;
             y < first.Height;
             y++)
        {
            for (int x = 0;
                 x < first.Width;
                 x++)
            {
                if (first.Cells[y, x] !=
                    second.Cells[y, x])
                {
                    return false;
                }
            }
        }

        return true;
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
                _width -
                piece.Width;

            int maxY =
                _height -
                piece.Height;

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
                                boardY *
                                _width +
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
                            new Position(
                                x,
                                y),
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
    // CREATE NORMAL SOLUTION
    // =========================================================

    private Solution CreateSolution(
        Position[] positions)
    {
        if (positions.Length !=
            _pieceCount)
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
            CloneBoard(
                board));

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            ApplyPieceToBoard(
                board,
                _puzzle.Pieces[piece],
                positions[piece]);

            states.Add(
                CloneBoard(
                    board));
        }

        VerifyBoardSolved(
            board);

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
                    position.X +
                    px;

                int y =
                    position.Y +
                    py;

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
            new int[
                height,
                width];

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