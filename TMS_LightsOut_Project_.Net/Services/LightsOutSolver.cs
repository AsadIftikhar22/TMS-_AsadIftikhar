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

    private const int DefaultTimeLimitSeconds = 15;

    // Extra time used only when the first solve times out.
    private const int FallbackTimeLimitSeconds = 90;

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
            $"Elapsed: " +
            $"{_stopwatch.Elapsed.TotalSeconds:F2}s";
    }

    // =========================================================
    // MAIN SOLVE
    // =========================================================

    public Solution? Solve()
    {
        _stopwatch.Restart();

        _diagnostic = string.Empty;
        _branches = 0;
        _conflicts = 0;

        try
        {
            CpModel model =
                BuildModel(out BoolVar[][] selected);

            CpSolver solver =
                CreateSolver(
                    _timeLimitSeconds);

            CpSolverStatus status =
                solver.Solve(model);

            _branches =
                solver.NumBranches();

            _conflicts =
                solver.NumConflicts();

            // =================================================
            // NORMAL SUCCESS
            // =================================================

            if (status == CpSolverStatus.Optimal ||
                status == CpSolverStatus.Feasible)
            {
                Position[] positions =
                    ExtractPositions(
                        solver,
                        selected);

                _stopwatch.Stop();

                //
                // First try to create the complete diagram.
                //
                try
                {
                    Solution solution =
                        CreateSolutionWithDiagram(
                            positions);

                    _diagnostic =
                        $"Solved successfully.\n" +
                        $"Time: " +
                        $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                        $"Branches: {_branches:N0}\n" +
                        $"Conflicts: {_conflicts:N0}\n" +
                        $"Diagram: Yes";

                    return solution;
                }
                catch
                {
                    //
                    // We already have a valid set of positions.
                    //
                    // If diagram creation fails, return the
                    // answer without the diagram.
                    //

                    Solution solution =
                        CreateSolutionWithoutDiagram(
                            positions);

                    _diagnostic =
                        $"Solved successfully.\n" +
                        $"Time: " +
                        $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                        $"Branches: {_branches:N0}\n" +
                        $"Conflicts: {_conflicts:N0}\n" +
                        $"Diagram: No\n" +
                        $"Solution positions returned only.";

                    return solution;
                }
            }

            // =================================================
            // TIMEOUT / UNKNOWN
            // =================================================

            if (status == CpSolverStatus.Unknown)
            {
                //
                // The first solve did not finish.
                //
                // Run a fallback solve.
                //
                _diagnostic =
                    $"Initial solve timed out.\n" +
                    $"Trying fallback search...";

                CpModel fallbackModel =
                    BuildModel(
                        out BoolVar[][] fallbackSelected);

                CpSolver fallbackSolver =
                    CreateSolver(
                        FallbackTimeLimitSeconds);

                CpSolverStatus fallbackStatus =
                    fallbackSolver.Solve(
                        fallbackModel);

                _branches +=
                    fallbackSolver.NumBranches();

                _conflicts +=
                    fallbackSolver.NumConflicts();

                if (fallbackStatus ==
                        CpSolverStatus.Optimal ||
                    fallbackStatus ==
                        CpSolverStatus.Feasible)
                {
                    Position[] positions =
                        ExtractPositions(
                            fallbackSolver,
                            fallbackSelected);

                    _stopwatch.Stop();

                    //
                    // IMPORTANT:
                    //
                    // Do NOT build the diagram after a timeout.
                    //
                    // Return positions only.
                    //

                    Solution solution =
                        CreateSolutionWithoutDiagram(
                            positions);

                    _diagnostic =
                        $"Solved using fallback search.\n" +
                        $"Initial search timed out.\n" +
                        $"Time: " +
                        $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                        $"Branches: {_branches:N0}\n" +
                        $"Conflicts: {_conflicts:N0}\n" +
                        $"Diagram: No\n" +
                        $"Solution positions returned only.";

                    return solution;
                }

                _stopwatch.Stop();

                _diagnostic =
                    $"Solver timed out.\n\n" +
                    $"Initial time limit: " +
                    $"{_timeLimitSeconds}s\n" +
                    $"Fallback time limit: " +
                    $"{FallbackTimeLimitSeconds}s\n" +
                    $"Total time: " +
                    $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                    $"Branches: {_branches:N0}\n" +
                    $"Conflicts: {_conflicts:N0}";

                return null;
            }

            // =================================================
            // INFEASIBLE
            // =================================================

            _stopwatch.Stop();

            _diagnostic =
                $"No solution exists.\n\n" +
                $"OR-Tools status: {status}\n" +
                $"Time: " +
                $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                $"Branches: {_branches:N0}\n" +
                $"Conflicts: {_conflicts:N0}";

            return null;
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

        for (int pieceIndex = 0;
             pieceIndex < _pieceCount;
             pieceIndex++)
        {
            int placementCount =
                _placements[pieceIndex].Count;

            selected[pieceIndex] =
                new BoolVar[placementCount];

            for (int placementIndex = 0;
                 placementIndex < placementCount;
                 placementIndex++)
            {
                selected[pieceIndex][placementIndex] =
                    model.NewBoolVar(
                        $"P{pieceIndex}_Placement{placementIndex}");
            }

            model.AddExactlyOne(
                selected[pieceIndex]);
        }

        // =====================================================
        // CELL CONSTRAINTS
        // =====================================================

        for (int cell = 0;
             cell < _cellCount;
             cell++)
        {
            var hits =
                new List<BoolVar>();

            for (int pieceIndex = 0;
                 pieceIndex < _pieceCount;
                 pieceIndex++)
            {
                for (int placementIndex = 0;
                     placementIndex <
                     _placements[pieceIndex].Count;
                     placementIndex++)
                {
                    Placement placement =
                        _placements[
                            pieceIndex][
                            placementIndex];

                    if (Array.BinarySearch(
                            placement.Cells,
                            cell) >= 0)
                    {
                        hits.Add(
                            selected[
                                pieceIndex][
                                placementIndex]);
                    }
                }
            }

            int initialValue =
                GetInitialCellValue(cell);

            // No piece can affect this cell.
            if (hits.Count == 0)
            {
                if (initialValue != 0)
                {
                    model.Add(
                        LinearExpr.Constant(1) == 0);
                }

                continue;
            }

            int requiredHits =
                (
                    _depth -
                    initialValue
                ) % _depth;

            LinearExpr totalHits =
                LinearExpr.Sum(hits);

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
                requiredHits);
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
        int timeLimitSeconds)
    {
        CpSolver solver =
            new CpSolver();

        solver.StringParameters =
            string.Join(
                " ",
                $"max_time_in_seconds:{timeLimitSeconds}",
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

        for (int pieceIndex = 0;
             pieceIndex < _pieceCount;
             pieceIndex++)
        {
            int selectedPlacementIndex =
                -1;

            for (int placementIndex = 0;
                 placementIndex <
                 selected[pieceIndex].Length;
                 placementIndex++)
            {
                if (solver.Value(
                        selected[
                            pieceIndex][
                            placementIndex]) >
                    0)
                {
                    selectedPlacementIndex =
                        placementIndex;

                    break;
                }
            }

            if (selectedPlacementIndex < 0)
            {
                throw new InvalidOperationException(
                    $"No placement selected for " +
                    $"piece {pieceIndex + 1}.");
            }

            positions[pieceIndex] =
                _placements[
                    pieceIndex][
                    selectedPlacementIndex]
                    .Position;
        }

        return positions;
    }

    // =========================================================
    // NORMAL SOLUTION
    // =========================================================

    private Solution CreateSolutionWithDiagram(
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

        for (int pieceIndex = 0;
             pieceIndex < _pieceCount;
             pieceIndex++)
        {
            ApplyPieceToBoard(
                board,
                _puzzle.Pieces[pieceIndex],
                positions[pieceIndex]);

            states.Add(
                CloneBoard(board));
        }

        VerifyBoardSolved(board);

        return new Solution(
            positions,
            states);
    }

    // =========================================================
    // POSITIONS-ONLY SOLUTION
    // =========================================================

    private Solution CreateSolutionWithoutDiagram(
        Position[] positions)
    {
        if (positions.Length != _pieceCount)
        {
            throw new InvalidOperationException(
                "Incorrect number of solution positions.");
        }

        //
        // Still verify the answer, but create only ONE board.
        //

        int[,] board =
            CloneBoard(
                _puzzle.InitialBoard);

        for (int pieceIndex = 0;
             pieceIndex < _pieceCount;
             pieceIndex++)
        {
            ApplyPieceToBoard(
                board,
                _puzzle.Pieces[pieceIndex],
                positions[pieceIndex]);
        }

        VerifyBoardSolved(board);

        //
        // Empty states means:
        //
        // "Do not render diagram."
        //

        return new Solution(
            positions,
            new List<int[,]>());
    }

    // =========================================================
    // SYMMETRY BREAKING
    // =========================================================

    private void AddIdenticalPieceSymmetryBreaking(
        CpModel model,
        BoolVar[][] selected)
    {
        for (int firstPiece = 0;
             firstPiece < _pieceCount;
             firstPiece++)
        {
            for (int secondPiece = firstPiece + 1;
                 secondPiece < _pieceCount;
                 secondPiece++)
            {
                if (!PiecesAreIdentical(
                        _puzzle.Pieces[firstPiece],
                        _puzzle.Pieces[secondPiece]))
                {
                    continue;
                }

                int firstCount =
                    selected[firstPiece].Length;

                int secondCount =
                    selected[secondPiece].Length;

                int maxBoundary =
                    Math.Min(
                        firstCount,
                        secondCount);

                for (int boundary = 1;
                     boundary < maxBoundary;
                     boundary++)
                {
                    var firstHigh =
                        new List<ILiteral>();

                    for (int i = boundary;
                         i < firstCount;
                         i++)
                    {
                        firstHigh.Add(
                            selected[
                                firstPiece][i]);
                    }

                    var secondLow =
                        new List<ILiteral>();

                    for (int j = 0;
                         j < boundary;
                         j++)
                    {
                        secondLow.Add(
                            selected[
                                secondPiece][j]);
                    }

                    if (firstHigh.Count == 0 ||
                        secondLow.Count == 0)
                    {
                        continue;
                    }

                    model.AddAtMostOne(
                        firstHigh
                            .Concat(secondLow)
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