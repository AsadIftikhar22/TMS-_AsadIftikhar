// ===========================================================================
// LightsOutSolver
//
// This class solves the Lights Out puzzle by turning every legal piece
// placement into a CP-SAT decision. The comments below explain the purpose
// of each important field, method, and constraint in plain language.
// ===========================================================================

// OR-Tools CP-SAT is used to find a valid placement for every puzzle piece.
using Google.OrTools.Sat;
// Contains the Puzzle, Piece, Position, and Solution models used by this solver.
using LightsOut.Wpf.Models;
// Stopwatch is used to measure how long each solving attempt takes.
using System.Diagnostics;

namespace LightsOut.Wpf.Services;

// Main solver class: builds the mathematical puzzle model, runs OR-Tools, and converts the result into a Solution.
public sealed class LightsOutSolver
{
    // The puzzle being solved, including its board, pieces, dimensions, and depth.
    private readonly Puzzle _puzzle;

    private readonly int _height;
    private readonly int _width;
    private readonly int _depth;
    private readonly int _pieceCount;
    private readonly int _cellCount;

    // For each piece, stores every board position where that piece can legally be placed.
    private readonly List<Placement>[] _placements;

    // Maximum time allowed for the normal CP-SAT search.
    private readonly int _timeLimitSeconds;

    // Measures the duration of the normal solving operation.
    private readonly Stopwatch _stopwatch = new();

    // Human-readable information about the last solve attempt, useful for the UI.
    private string _diagnostic = string.Empty;

    // Number of search branches explored by OR-Tools.
    private long _branches;
    // Number of conflicts detected by OR-Tools while searching.
    private long _conflicts;

    // True when the normal solver stopped because its time limit was reached.
    private bool _wasTimeout;

    // Default normal-search limit: five minutes.
    private const int DefaultTimeLimitSeconds = 450;

    //
    // Fallback gets additional time, but does NOT create
    // diagram states.
    //
    // Additional five-minute limit for the answer-only fallback search.
    private const int FallbackTimeLimitSeconds = 450;

    // Creates a solver using the default five-minute search limit.
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
        // Keep the supplied puzzle and fail immediately if no puzzle was provided.
        _puzzle =
            puzzle ??
            throw new ArgumentNullException(
                nameof(puzzle));

        // A zero or negative timeout would make the solver configuration invalid.
        if (timeLimitSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeLimitSeconds));
        }

        // Store the configured search limit for use by Solve().
        _timeLimitSeconds =
            timeLimitSeconds;

        // Board height in cells.
        _height =
            puzzle.Height;

        // Board width in cells.
        _width =
            puzzle.Width;

        // Number of states each cell can cycle through before returning to zero.
        _depth =
            puzzle.Depth;

        // Total number of pieces that must each be placed exactly once.
        _pieceCount =
            puzzle.Pieces.Count;

        // Total number of cells on the board.
        _cellCount =
            checked(
                _height *
                _width);

        // Reject boards that have no usable rows or columns.
        if (_height <= 0 ||
            _width <= 0)
        {
            throw new ArgumentException(
                "Invalid board dimensions.");
        }

        // Protect the solver from boards larger than the supported search/model size.
        if (_cellCount > 100)
        {
            throw new ArgumentException(
                "Maximum supported board size is 100 cells.");
        }

        // The puzzle requires at least two states and supports up to five states per cell.
        if (_depth < 2 ||
            _depth > 5)
        {
            throw new ArgumentException(
                "Depth must be between 2 and 5.");
        }

        // A puzzle without pieces cannot produce a placement solution.
        if (_pieceCount == 0)
        {
            throw new ArgumentException(
                "Puzzle contains no pieces.");
        }

        // Precalculate all legal positions for every piece before building the CP-SAT model.
        _placements =
            BuildPlacements();
    }

    // =========================================================
    // DIAGNOSTIC
    // =========================================================

    // Returns the most useful status/details from the latest solve attempt.
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

    // Lets the UI distinguish a timeout from a proven unsatisfiable puzzle.
    public bool WasTimeout =>
        _wasTimeout;

    // =========================================================
    // NORMAL SOLVE
    // =========================================================

    // Performs the normal solve and, when successful, creates all intermediate board states for display.
    public Solution? Solve()
    {
        // Start timing this solve from the beginning.
        _stopwatch.Restart();

        // Report successful completion and useful performance statistics.
        _diagnostic =
            string.Empty;

        // Reset OR-Tools branch statistics.
        _branches = 0;

        // Reset OR-Tools conflict statistics.
        _conflicts = 0;

        // A new solve starts with no timeout recorded.
        _wasTimeout = false;

        try
        {
            // Start with an empty constraint model.
            CpModel model =
                BuildModel(
                    out BoolVar[][] selected);

            // Create and configure the OR-Tools search engine.
            CpSolver solver =
                CreateSolver(
                    _timeLimitSeconds);

            // Run CP-SAT and record whether it found a solution, proved no solution, or timed out.
            CpSolverStatus status =
                solver.Solve(model);

            // Save search statistics before the solver object goes out of scope.
            _branches =
                solver.NumBranches();

            // Save conflict statistics for diagnostics.
            _conflicts =
                solver.NumConflicts();

            _stopwatch.Stop();

            // Only Optimal and Feasible mean that OR-Tools returned a usable solution.
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

                    // Report successful completion and useful performance statistics.
                    _diagnostic =
                        $"Normal solver reached its " +
                        $"time limit.\n\n" +
                        $"Time: " +
                        $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                        $"Branches: {_branches:N0}\n" +
                        $"Conflicts: {_conflicts:N0}";

                    return null;
                }

                // Report successful completion and useful performance statistics.
                _diagnostic =
                    $"No solution exists.\n\n" +
                    $"OR-Tools status: {status}\n" +
                    $"Time: " +
                    $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                    $"Branches: {_branches:N0}\n" +
                    $"Conflicts: {_conflicts:N0}";

                return null;
            }

            // Convert OR-Tools boolean choices into the board position of each piece.
            Position[] positions =
                ExtractPositions(
                    solver,
                    selected);

            // Build the final solution, including the intermediate board diagrams.
            Solution solution =
                CreateSolution(
                    positions);

            // Report successful completion and useful performance statistics.
            _diagnostic =
                $"Solved successfully.\n" +
                $"Time: " +
                $"{_stopwatch.Elapsed.TotalSeconds:F2}s\n" +
                $"Branches: {_branches:N0}\n" +
                $"Conflicts: {_conflicts:N0}";

            return solution;
        }
        // Convert unexpected solver errors into diagnostic information instead of crashing the UI.
        catch (Exception ex)
        {
            _stopwatch.Stop();

            // Report successful completion and useful performance statistics.
            _diagnostic =
                $"Solver exception.\n\n" +
                $"{ex}";

            return null;
        }
    }

    // =========================================================
    // ANSWER-ONLY FALLBACK
    // =========================================================

    // Retry after a normal timeout, but return only piece positions to avoid building expensive diagram states.
    public Solution? SolveAnswerOnly()
    {
        // Time the fallback independently from the normal search.
        Stopwatch fallbackWatch =
            Stopwatch.StartNew();

        try
        {
            // Start with an empty constraint model.
            CpModel model =
                BuildModel(
                    out BoolVar[][] selected);

            // Create and configure the OR-Tools search engine.
            CpSolver solver =
                CreateSolver(
                    FallbackTimeLimitSeconds);

            // Run CP-SAT and record whether it found a solution, proved no solution, or timed out.
            CpSolverStatus status =
                solver.Solve(model);

            // Capture branch statistics for the fallback attempt.
            long fallbackBranches =
                solver.NumBranches();

            // Capture conflict statistics for the fallback attempt.
            long fallbackConflicts =
                solver.NumConflicts();

            fallbackWatch.Stop();

            // Only Optimal and Feasible mean that OR-Tools returned a usable solution.
            if (status !=
                    CpSolverStatus.Optimal &&
                status !=
                    CpSolverStatus.Feasible)
            {
                // Report successful completion and useful performance statistics.
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

            // Convert OR-Tools boolean choices into the board position of each piece.
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

            // The fallback intentionally returns no intermediate board states because only the final answer is needed.
            var solution =
                new Solution(
                    positions,
                    Array.Empty<int[,]>());

            // Report successful completion and useful performance statistics.
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
        // Convert unexpected solver errors into diagnostic information instead of crashing the UI.
        catch (Exception ex)
        {
            fallbackWatch.Stop();

            // Report successful completion and useful performance statistics.
            _diagnostic =
                $"Fallback solver exception.\n\n" +
                $"{ex}";

            return null;
        }
    }

    // =========================================================
    // BUILD MODEL
    // =========================================================

    // Creates the CP-SAT model: one choice per piece plus constraints that make every board cell reach zero.
    private CpModel BuildModel(
        out BoolVar[][] selected)
    {
        // Start with an empty constraint model.
        CpModel model =
            new CpModel();

        // Each piece gets an array of boolean variables, one variable for each legal placement.
        selected =
            new BoolVar[_pieceCount][];

        // =====================================================
        // Each boolean means: 'this piece is placed at this exact position'.
        // ONE BOOLEAN PER PLACEMENT
        // =====================================================

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            // Count how many legal positions this particular piece has.
            int count =
                _placements[piece].Count;

            // Allocate one boolean decision variable for every legal placement.
            selected[piece] =
                new BoolVar[count];

            for (int placement = 0;
                 placement < count;
                 placement++)
            {
                // Create the decision variable representing this specific piece/position combination.
                selected[piece][placement] =
                    model.NewBoolVar(
                        $"P{piece}_Placement{placement}");
            }

            // A piece must be placed in exactly one of its legal positions.
            model.AddExactlyOne(
                selected[piece]);
        }

        // =====================================================
        // Ensure the total number of piece hits on every cell produces the required final state.
        // CELL CONSTRAINTS
        // =====================================================

        for (int cell = 0;
             cell < _cellCount;
             cell++)
        {
            // Collect all placement choices that affect this particular board cell.
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

            // Read the cell's starting value so the required number of hits can be calculated.
            int initial =
                GetInitialCellValue(
                    cell);

            // If no piece can ever touch this cell, it must already be solved.
            if (hits.Count == 0)
            {
                if (initial != 0)
                {
                    // Require the calculated remainder to equal the number of hits needed.
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

            // Sum all selected placements that affect this cell.
            LinearExpr totalHits =
                LinearExpr.Sum(
                    hitVars);

            // Calculate how many hits are needed, modulo the puzzle depth, to bring this cell to zero.
            int required =
                (_depth - initial) %
                _depth;

            // CP-SAT variable holding the total number of hits reduced modulo the cell depth.
            IntVar remainder =
                model.NewIntVar(
                    0,
                    _depth - 1,
                    $"Remainder_{cell}");

            // Force the cell's hit count to wrap around according to the puzzle's depth.
            model.AddModuloEquality(
                remainder,
                totalHits,
                _depth);

            // Require the calculated remainder to equal the number of hits needed.
            model.Add(
                remainder ==
                required);
        }

        // =====================================================
        // Remove equivalent search paths when two pieces have exactly the same shape.
        // SYMMETRY
        // =====================================================

        // Add constraints that prevent OR-Tools from exploring duplicate solutions caused by interchangeable pieces.
        AddIdenticalPieceSymmetryBreaking(
            model,
            selected);

        return model;
    }

    // =========================================================
    // CREATE SOLVER
    // =========================================================

    // Configure the CP-SAT engine with timing, parallel search, presolve, probing, and symmetry settings.
    private static CpSolver CreateSolver(
        int seconds)
    {
        // Create the OR-Tools CP-SAT solver instance.
        var solver =
            new CpSolver();

        // Configure OR-Tools using its standard parameter string.
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

    // Read the selected placement variables and return one board position per puzzle piece.
    private Position[] ExtractPositions(
        CpSolver solver,
        BoolVar[][] selected)
    {
        // Result array: index = piece number, value = the chosen board position.
        var positions =
            new Position[_pieceCount];

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            // Will hold the index of the placement chosen by OR-Tools.
            int selectedPlacement =
                -1;

            for (int placement = 0;
                 placement <
                 selected[piece].Length;
                 placement++)
            {
                // Check whether OR-Tools selected this particular placement.
                if (solver.Value(
                        selected[piece][placement]) >
                    0)
                {
                    selectedPlacement =
                        placement;

                    break;
                }
            }

            // A valid CP-SAT solution should always select exactly one placement per piece.
            if (selectedPlacement < 0)
            {
                throw new InvalidOperationException(
                    $"No placement selected for " +
                    $"piece {piece + 1}.");
            }

            // Convert the selected placement index into its actual X/Y board position.
            positions[piece] =
                _placements[piece]
                    [selectedPlacement]
                    .Position;
        }

        return positions;
    }

    // =========================================================
    // Prevent identical pieces from being swapped in equivalent ways during the search.
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
                // Only apply symmetry breaking when the two pieces really have the same shape and size.
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

                    // Prevent the two identical pieces from choosing equivalent placement combinations in the same symmetry region.
                    model.AddAtMostOne(
                        firstHigh
                            .Concat(secondLower)
                            .ToArray());
                }
            }
        }
    }

    // =========================================================
    // Checks whether two pieces have exactly the same dimensions and occupied cells.
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
                // Compare every cell in the two piece shapes.
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

    // Converts a flat cell index into row/column coordinates and reads the starting value.
    private int GetInitialCellValue(
        int cell)
    {
        // Calculate the board Y coordinate for this piece cell.
        int y =
            cell / _width;

        // Calculate the board X coordinate for this piece cell.
        int x =
            cell % _width;

        // Return the puzzle's initial value for this board cell.
        return _puzzle.InitialBoard[y, x];
    }

    // =========================================================
    // Generates every legal position for every piece before solving begins.
    // BUILD PLACEMENTS
    // =========================================================

    private List<Placement>[] BuildPlacements()
    {
        // Allocate an independent board for the copied values.
        var result =
            new List<Placement>[_pieceCount];

        for (int pieceIndex = 0;
             pieceIndex < _pieceCount;
             pieceIndex++)
        {
            // Get the shape of the current piece.
            Piece piece =
                _puzzle.Pieces[pieceIndex];

            // Last X coordinate where the entire piece can fit horizontally.
            int maxX =
                _width -
                piece.Width;

            // Last Y coordinate where the entire piece can fit vertically.
            int maxY =
                _height -
                piece.Height;

            // Reject a piece that is larger than the board in either direction.
            if (maxX < 0 ||
                maxY < 0)
            {
                throw new InvalidOperationException(
                    $"Piece {pieceIndex + 1} " +
                    $"({piece.Width}×{piece.Height}) " +
                    $"is larger than board " +
                    $"({_width}×{_height}).");
            }

            // Store every legal placement found for this piece.
            var placements =
                new List<Placement>();

            // Give each legal placement a stable numeric identifier.
            int index = 0;

            for (int y = 0;
                 y <= maxY;
                 y++)
            {
                for (int x = 0;
                     x <= maxX;
                     x++)
                {
                    // Store the board cell indexes covered by this placement.
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
                            // Empty cells in the piece shape do not affect the board.
                            if (!piece.Cells[py, px])
                            {
                                continue;
                            }

                            // Convert the piece's local X coordinate into a board X coordinate.
                            int boardX =
                                x + px;

                            // Convert the piece's local Y coordinate into a board Y coordinate.
                            int boardY =
                                y + py;

                            // Ignore shape cells that would fall outside the board.
                            if (boardX < 0 ||
                                boardX >= _width ||
                                boardY < 0 ||
                                boardY >= _height)
                            {
                                continue;
                            }

                            // Store the affected board cell as one flat integer index.
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

                    // Sort cell indexes so later binary searches can be used efficiently.
                    cells.Sort();

                    // Save this valid position and the board cells it affects.
                    placements.Add(
                        new Placement(
                            index++,
                            new Position(
                                x,
                                y),
                            cells.ToArray()));
                }
            }

            // A piece with no legal position makes the puzzle impossible to solve.
            if (placements.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Piece {pieceIndex + 1} " +
                    "has no valid placements.");
            }

            // Store all legal placements for this piece.
            result[pieceIndex] =
                placements;
        }

        return result;
    }

    // =========================================================
    // Converts the selected piece positions into the complete sequence of board states shown by the UI.
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

        // Holds the initial board plus the board after every piece is applied.
        var states =
            new List<int[,]>(
                _pieceCount + 1);

        // Work on a copy so the original puzzle input is never modified.
        int[,] board =
            CloneBoard(
                _puzzle.InitialBoard);

        // First state is the original board before any piece is placed.
        states.Add(
            CloneBoard(
                board));

        for (int piece = 0;
             piece < _pieceCount;
             piece++)
        {
            // Apply the selected piece to the working board.
            ApplyPieceToBoard(
                board,
                _puzzle.Pieces[piece],
                positions[piece]);

            // First state is the original board before any piece is placed.
            states.Add(
                CloneBoard(
                    board));
        }

        // Confirm that the final board is actually solved before returning it.
        VerifyBoardSolved(
            board);

        return new Solution(
            positions,
            states);
    }

    // =========================================================
    // Adds one piece's effect to every occupied board cell, wrapping values by depth.
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
                // Empty cells in the piece shape do not affect the board.
                if (!piece.Cells[py, px])
                {
                    continue;
                }

                // Calculate the board X coordinate for this piece cell.
                int x =
                    position.X +
                    px;

                // Calculate the board Y coordinate for this piece cell.
                int y =
                    position.Y +
                    py;

                // Never silently allow a selected piece to modify a cell outside the board.
                if (x < 0 ||
                    x >= _width ||
                    y < 0 ||
                    y >= _height)
                {
                    throw new InvalidOperationException(
                        $"Piece placed outside board " +
                        $"at ({x},{y}).");
                }

                // Increase the cell value and wrap it back to zero when it reaches the depth.
                board[y, x] =
                    (board[y, x] + 1) %
                    _depth;
            }
        }
    }

    // =========================================================
    // Final safety check: every board cell must be zero after all pieces are applied.
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
                // Any non-zero cell means the solver result does not actually solve the puzzle.
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
    // Creates a deep copy of a board so saved solution states cannot affect each other.
    // CLONE
    // =========================================================

    private static int[,] CloneBoard(
        int[,] source)
    {
        // Read the number of rows from the source board.
        int height =
            source.GetLength(0);

        // Read the number of columns from the source board.
        int width =
            source.GetLength(1);

        // Allocate an independent board for the copied values.
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
                // Copy each individual cell into the new board.
                result[y, x] =
                    source[y, x];
            }
        }

        return result;
    }

    // =========================================================
    // Small internal data object describing one legal placement of a piece.
    // PLACEMENT
    // =========================================================

    private sealed class Placement
    {
        // Creates a placement record containing its ID, position, and affected board cells.
        public Placement(
            int index,
            Position position,
            int[] cells)
        {
            // Unique index of this placement within the piece's placement list.
            Index =
                index;

            // X/Y location where the piece is placed on the board.
            Position =
                position;

            // Flat board indexes affected by this placement.
            Cells =
                cells;
        }

        public int Index { get; }

        public Position Position { get; }

        public int[] Cells { get; }
    }
}
