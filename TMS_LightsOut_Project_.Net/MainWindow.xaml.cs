using LightsOut.Wpf.Models;
using LightsOut.Wpf.Services;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LightsOut.Wpf;

public partial class MainWindow : Window
{
    // =========================================================
    // SAMPLE INPUT
    //
    // 10 samples.
    //
    // Each sample:
    //
    // depth
    // board
    // pieces
    // =========================================================

    private const string SampleInput =
"""
2
100,101,011
..X,XXX,X.. X,X,X .X,XX XX.,.X.,.XX XX,X. XX .XX,XX.

4
3302012,3221112,3121312,1312033,0201003,0101102,0221020,0302223,0000000
.X,XX,X.,X. .XXXX,.XXXX,XXXXX,..X.. XX..,XXXX,XXX.,XXXX X.,X.,X.,XX .X...,XXXX.,...X.,...XX,...X. X..,X..,X.X,XXX,XXX ..XX,.XXX,.X.X,.X..,XX.. XXX,XX. XXX.,.XXX,.XXX,.XXX,...X .XX.X,.XXXX,XXXXX,..X.. ..XX,..XX,.XX.,.XX.,XXX. .XXX,XX.. ..X..,..XXX,..XXX,XXXX.,..X.. X.X..,XXX..,XXX..,XXXXX,X.X.. XX..,.XXX .XX..,XXXX.,.XXX.,.XXXX,.X... XXXXX,....X

2
10010010,00101010,01111100,11111110,10101001,01001000
.X,XX,XX,X. .XX,XX. XXXXX,XXXX.,.XXX.,..XX.,..X.. X,X,X,X,X ..X..,XXXXX ..XX.,..XXX,XXXX.,XXXXX,X.... .X,XX ...X.,..XXX,XXXXX,.XX..,..X.. XXX.,.XXX,.XXX,XX.X,X... XX,X. XXX..,.XX..,..X..,.XX..,..XXX X.XXX,XXXX.,.XXXX,XXX.. XX,X.,X.,XX,.X X,X,X ..X..,XXXXX,..X.. XXXXX,XXX.X,XX...

2
100000,011101,101100,110000,011000
.X,XX,X. XXX XXX,XX. .X.,.X.,.X.,XXX,.X. X.,XX,XX .X.,XXX,X.. X...,XXXX X..,X..,XXX,.X. XXX,.X.,.X. .X.,.X.,XX.,.XX .X,XX,X. X.XXX,XXX.. XX.,.X.,.X.,.XX .X,XX,.X X.,X.,XX

4
132330,230323,301031,223121,332313
.X,.X,XX,X.,X. ..XX,XXXX,..X. XX.,.XX .XX,..X,XXX,X.. .X.,.X.,XXX,..X .X,.X,.X,XX,X. XX..,.XXX XX,.X,XX XX,XX X...,XXXX,...X,...X .X,.X,XX,X. .X,.X,XX XX,XX,.X XXX,..X,..X,..X

4
01230,00130,33203,02131,23313,03010,33320
XXX.,.X..,.XXX,..X.,..X. X.XX.,XX.XX,XXXX. XX.XX,XXXX.,..XX. ...XX,...XX,...X.,XXXX. .XX.,.XXX,XXX.,.X.. XXXX,...X .X,XX,X. .X,XX,X. .X..,XX..,XXXX,.X.. .XX,.X.,XXX,XXX,..X .X.,.XX,.XX,XX.,.X. .X..,XX..,.XXX

3
2121,2212,1001,2011,1211,2111
X...,X.X.,XXXX,XX.. XXX,X.X XX,X. XX,.X,.X XX.,.XX,.X.,.X. XX..,.XXX .X.,.X.,XX.,XXX ..XX,..XX,XXX. .X..,.XX.,X.X.,XXXX X,X,X,X .X.,.XX,.X.,.X.,XXX

3
110102,001200,110221,020120
..X,..X,..X,XXX ..X,XXX,..X XXXXX,X...X XX,.X,.X,.X XXX .X,.X,XX XXX,X.. XX.,.XX .X.,XX.,.XX,XX. X..,XXX,..X XX,X.,XX ..X..,XXXXX

3
102020,120110,001002,100222,112022
.X..,.XX.,XXX.,..X.,..XX ..X,XXX,..X ..X,.XX,XX.,X..,X.. X..,XX.,XXX,XXX,X.. .X...,XXXXX,...X.,...X.,...X. XX.,.XX,.XX,.X.,XX. .XX.,.XX.,XXX.,..XX X.X.,XXXX,X... X.,X.,XX,XX .X.,XXX,.XX,.X. .X,XX,XX

2
0100,0110,1010,1110
X.,XX,XX X...,XXXX XXX X,X XX XX,XX,.X,.X ..XX,XXX.
""";

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    // =========================================================
    // LOADED
    // =========================================================

    private void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        LoadSamples();
    }

    // =========================================================
    // LOAD ALL SAMPLES
    // =========================================================

    private void LoadSamples()
    {
        SamplesPanel.Children.Clear();

        try
        {
            IReadOnlyList<Puzzle> puzzles =
                PuzzleParser.ParseMany(
                    SampleInput);

            OverallStatusText.Text =
                $"{puzzles.Count} samples loaded.";

            for (int i = 0;
                 i < puzzles.Count;
                 i++)
            {
                AddSampleCard(
                    i + 1,
                    puzzles[i]);
            }
        }
        catch (Exception ex)
        {
            OverallStatusText.Text =
                "Failed to load samples.";

            var error =
                new TextBlock
                {
                    Text =
                        ex.ToString(),

                    Foreground =
                        Brushes.Firebrick,

                    TextWrapping =
                        TextWrapping.Wrap,

                    Margin =
                        new Thickness(10)
                };

            SamplesPanel.Children.Add(
                error);
        }
    }

    // =========================================================
    // SAMPLE CARD
    // =========================================================

    private void AddSampleCard(
        int sampleNumber,
        Puzzle puzzle)
    {
        var outerBorder =
            new Border
            {
                Background =
                    Brushes.White,

                BorderBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            210,
                            215,
                            222)),

                BorderThickness =
                    new Thickness(1),

                CornerRadius =
                    new CornerRadius(8),

                Padding =
                    new Thickness(20),

                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        24)
            };

        var mainPanel =
            new StackPanel();

        // =====================================================
        // HEADER
        // =====================================================

        var headerGrid =
            new Grid();

        headerGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(1, GridUnitType.Star)
            });

        headerGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });

        var titlePanel =
            new StackPanel();

        titlePanel.Children.Add(
            new TextBlock
            {
                Text =
                    $"Sample {sampleNumber}",

                FontSize = 22,

                FontWeight =
                    FontWeights.Bold,

                Foreground =
                    (Brush)FindResource(
                        "TextBrush")
            });

        titlePanel.Children.Add(
            new TextBlock
            {
                Text =
                    $"Depth: {puzzle.Depth}   |   " +
                    $"Board: {puzzle.Height} × {puzzle.Width}   |   " +
                    $"Cells: {puzzle.Height * puzzle.Width}   |   " +
                    $"Pieces: {puzzle.Pieces.Count}",

                Margin =
                    new Thickness(
                        0,
                        5,
                        0,
                        0),

                Foreground =
                    (Brush)FindResource(
                        "MutedBrush")
            });

        Grid.SetColumn(
            titlePanel,
            0);

        headerGrid.Children.Add(
            titlePanel);

        // =====================================================
        // BUTTON
        // =====================================================

        var solveButton =
            new Button
            {
                Content =
                    "Solve Sample",

                Background =
                    (Brush)FindResource(
                        "DocumentBlueLight"),

                Foreground =
                    Brushes.White,

                Padding =
                    new Thickness(
                        24,
                        10,
                        24,
                        10),

                FontSize = 15,

                Tag =
                    new SampleContext
                    {
                        SampleNumber =
                            sampleNumber,

                        Puzzle =
                            puzzle,

                        StatusText =
                            null,

                        CoordinatesText =
                            null,

                        SolutionPanel =
                            null
                    }
            };

        solveButton.Click +=
            SolveSample_Click;

        Grid.SetColumn(
            solveButton,
            1);

        headerGrid.Children.Add(
            solveButton);

        mainPanel.Children.Add(
            headerGrid);

        // =====================================================
        // SEPARATOR
        // =====================================================

        mainPanel.Children.Add(
            new Border
            {
                Height = 1,

                Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            230,
                            230,
                            230)),

                Margin =
                    new Thickness(
                        0,
                        15,
                        0,
                        15)
            });

        // =====================================================
        // PROBLEM SECTION
        // =====================================================

        mainPanel.Children.Add(
            CreateSectionTitle(
                "Problem"));

        var problemGrid =
            new Grid();

        problemGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        0.45,
                        GridUnitType.Star)
            });

        problemGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        0.55,
                        GridUnitType.Star)
            });

        // BOARD

        var boardPanel =
            new StackPanel
            {
                Margin =
                    new Thickness(
                        0,
                        0,
                        25,
                        0)
            };

        boardPanel.Children.Add(
            new TextBlock
            {
                Text = "Initial Board",

                FontWeight =
                    FontWeights.SemiBold,

                FontSize = 15,

                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        8)
            });

        boardPanel.Children.Add(
            CreateBoardDiagram(
                puzzle.InitialBoard,
                puzzle.Depth,
                false));

        Grid.SetColumn(
            boardPanel,
            0);

        problemGrid.Children.Add(
            boardPanel);

        // PIECES

        var piecesPanel =
            new WrapPanel
            {
                Orientation =
                    Orientation.Horizontal
            };

        piecesPanel.Children.Add(
            new TextBlock
            {
                Text = "Pieces",

                FontWeight =
                    FontWeights.SemiBold,

                FontSize = 15,

                Width = 700,

                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        8)
            });

        for (int i = 0;
             i < puzzle.Pieces.Count;
             i++)
        {
            var pieceWrapper =
                new StackPanel
                {
                    Margin =
                        new Thickness(
                            0,
                            0,
                            18,
                            15)
                };

            pieceWrapper.Children.Add(
                CreatePieceGrid(
                    puzzle.Pieces[i]));

            pieceWrapper.Children.Add(
                new TextBlock
                {
                    Text =
                        $"Piece {i + 1}",

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    FontFamily =
                        new FontFamily(
                            "Georgia"),

                    FontSize = 13,

                    Margin =
                        new Thickness(
                            0,
                            5,
                            0,
                            0)
                });

            piecesPanel.Children.Add(
                pieceWrapper);
        }

        Grid.SetColumn(
            piecesPanel,
            1);

        problemGrid.Children.Add(
            piecesPanel);

        mainPanel.Children.Add(
            problemGrid);

        // =====================================================
        // STATUS
        // =====================================================

        var statusText =
            new TextBlock
            {
                Text =
                    "Ready to solve.",

                FontWeight =
                    FontWeights.SemiBold,

                Foreground =
                    (Brush)FindResource(
                        "MutedBrush"),

                Margin =
                    new Thickness(
                        0,
                        8,
                        0,
                        0),

                TextWrapping =
                    TextWrapping.Wrap
            };

        mainPanel.Children.Add(
            statusText);

        // =====================================================
        // COORDINATES
        // =====================================================

        var coordinatesText =
            new TextBlock
            {
                Text =
                    string.Empty,

                FontFamily =
                    new FontFamily(
                        "Consolas"),

                FontSize = 15,

                Foreground =
                    (Brush)FindResource(
                        "TextBrush"),

                Margin =
                    new Thickness(
                        0,
                        10,
                        0,
                        10),

                TextWrapping =
                    TextWrapping.Wrap
            };

        mainPanel.Children.Add(
            coordinatesText);

        // =====================================================
        // SOLUTION
        // =====================================================

        mainPanel.Children.Add(
            CreateSectionTitle(
                "Solution"));

        var solutionScroll =
            new ScrollViewer
            {
                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Auto,

                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Disabled,

                Margin =
                    new Thickness(
                        0,
                        5,
                        0,
                        0)
            };

        var solutionPanel =
            new StackPanel
            {
                Orientation =
                    Orientation.Horizontal
            };

        solutionScroll.Content =
            solutionPanel;

        mainPanel.Children.Add(
            solutionScroll);

        // =====================================================
        // SAVE REFERENCES
        // =====================================================

        var context =
            new SampleContext
            {
                SampleNumber =
                    sampleNumber,

                Puzzle =
                    puzzle,

                StatusText =
                    statusText,

                CoordinatesText =
                    coordinatesText,

                SolutionPanel =
                    solutionPanel,

                SolveButton =
                    solveButton
            };

        solveButton.Tag =
            context;

        // =====================================================
        // ADD CARD
        // =====================================================

        outerBorder.Child =
            mainPanel;

        SamplesPanel.Children.Add(
            outerBorder);
    }

    // =========================================================
    // SECTION TITLE
    // =========================================================

    private TextBlock CreateSectionTitle(
        string text)
    {
        return new TextBlock
        {
            Text = text,

            FontSize = 18,

            FontWeight =
                FontWeights.Bold,

            Foreground =
                (Brush)FindResource(
                    "TextBrush"),

            Margin =
                new Thickness(
                    0,
                    5,
                    0,
                    8)
        };
    }

    // =========================================================
    // SOLVE ONE SAMPLE
    // =========================================================

    private async void SolveSample_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not SampleContext context)
            return;

        if (context.IsSolving)
            return;

        context.IsSolving = true;

        button.IsEnabled = false;

        context.StatusText!.Text =
            "Solving...";

        context.StatusText.Foreground =
            (Brush)FindResource(
                "DocumentBlueLight");

        context.CoordinatesText!.Text =
            string.Empty;

        context.SolutionPanel!.Children.Clear();

        try
        {
            Puzzle puzzle =
                context.Puzzle;

            // =================================================
            // SOLVE ON BACKGROUND THREAD
            // =================================================

            SolverResult result =
                await Task.Run(
                    () =>
                    {
                        var solver =
                            new LightsOutSolver(
                                puzzle);

                        Solution? solution =
                            solver.Solve();

                        return new SolverResult
                        {
                            Solution =
                                solution,

                            Diagnostic =
                                solver.GetDiagnostic()
                        };
                    });

            if (result.Solution == null)
            {
                context.StatusText.Text =
                    "No solution found.";

                context.StatusText.Foreground =
                    Brushes.Firebrick;

                MessageBox.Show(
                    this,
                    $"Sample {context.SampleNumber}\n\n" +
                    result.Diagnostic,
                    "Lights Out Solver",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // =================================================
            // VERIFY
            // =================================================

            VerifySolution(
                puzzle,
                result.Solution);

            // =================================================
            // COORDINATES
            // =================================================

            context.CoordinatesText.Text =
                "Solution coordinates: " +
                string.Join(
                    "   ",
                    result.Solution.Positions.Select(
                        p =>
                            $"({p.X},{p.Y})"));

            // =================================================
            // ANIMATE
            // =================================================

            await AnimateSolution(
                context,
                result.Solution,
                puzzle);

            context.StatusText.Text =
                "Solved successfully. Final board is all 0.";

            context.StatusText.Foreground =
                (Brush)FindResource(
                    "GreenState");

            OverallStatusText.Text =
                $"Sample {context.SampleNumber} solved.";
        }
        catch (Exception ex)
        {
            context.StatusText!.Text =
                "Error while solving.";

            context.StatusText.Foreground =
                Brushes.Firebrick;

            MessageBox.Show(
                this,
                $"Sample {context.SampleNumber}\n\n" +
                ex,
                "Lights Out Solver Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            context.IsSolving = false;

            button.IsEnabled = true;
        }
    }

    // =========================================================
    // ANIMATE SOLUTION
    // =========================================================

    private async Task AnimateSolution(
        SampleContext context,
        Solution solution,
        Puzzle puzzle)
    {
        context.SolutionPanel!.Children.Clear();

        for (int i = 0;
             i < solution.States.Count;
             i++)
        {
            if (i > 0)
            {
                context.SolutionPanel.Children.Add(
                    new TextBlock
                    {
                        Text = "→",

                        FontSize = 30,

                        Foreground =
                            (Brush)FindResource(
                                "DocumentBlueLight"),

                        VerticalAlignment =
                            VerticalAlignment.Center,

                        Margin =
                            new Thickness(
                                12,
                                0,
                                12,
                                0)
                    });
            }

            string caption;

            if (i == 0)
            {
                caption =
                    "Initial";
            }
            else
            {
                Position position =
                    solution.Positions[i - 1];

                caption =
                    $"Piece {i}\n" +
                    $"({position.X},{position.Y})";
            }
            context.SolutionPanel.Children.Add(
                CreateBoardDiagram(
                    solution.States[i],
                    puzzle.Depth,
                    true,
                    caption));

            await Task.Delay(100);
        }
    }

    // =========================================================
    // BOARD DIAGRAM
    // =========================================================

    private FrameworkElement CreateBoardDiagram(
        int[,] board,
        int depth,
        bool showCaption,
        string caption = "")
    {
        var wrapper =
            new StackPanel
            {
                Margin =
                    new Thickness(
                        0,
                        0,
                        5,
                        0)
            };

        var grid =
            new Grid
            {
                HorizontalAlignment =
                    HorizontalAlignment.Left
            };

        int height =
            board.GetLength(0);

        int width =
            board.GetLength(1);

        for (int y = 0;
             y < height;
             y++)
        {
            grid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                });
        }

        for (int x = 0;
             x < width;
             x++)
        {
            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        GridLength.Auto
                });
        }

        int cellCount =
            height * width;

        double size =
            cellCount <= 36
                ? 38
                : cellCount <= 64
                    ? 31
                    : cellCount <= 100
                        ? 26
                        : 22;

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                Border cell =
                    CreateCell(
                        board[y, x],
                        depth,
                        size);

                Grid.SetRow(
                    cell,
                    y);

                Grid.SetColumn(
                    cell,
                    x);

                grid.Children.Add(
                    cell);
            }
        }

        var frame =
            new Border
            {
                BorderBrush =
                    (Brush)FindResource(
                        "DocumentBlueLight"),

                BorderThickness =
                    new Thickness(1),

                Padding =
                    new Thickness(5),

                Child =
                    grid
            };

        wrapper.Children.Add(
            frame);

        if (showCaption)
        {
            wrapper.Children.Add(
                new TextBlock
                {
                    Text = caption,

                    FontFamily =
                        new FontFamily(
                            "Georgia"),

                    FontSize = 12,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    TextAlignment =
                        TextAlignment.Center,

                    Margin =
                        new Thickness(
                            0,
                            5,
                            0,
                            0)
                });
        }

        return wrapper;
    }

    // =========================================================
    // CELL
    // =========================================================

    private Border CreateCell(
        int value,
        int depth,
        double size)
    {
        var border =
            new Border
            {
                Width = size,

                Height = size,

                BorderBrush =
                    (Brush)FindResource(
                        "DocumentBlueLight"),

                BorderThickness =
                    new Thickness(1),

                Background =
                    GetStateBrush(
                        value,
                        depth),

                CornerRadius =
                    new CornerRadius(2)
            };

        border.Child =
            new TextBlock
            {
                Text =
                    value.ToString(),

                FontFamily =
                    new FontFamily(
                        "Consolas"),

                FontSize =
                    size >= 35
                        ? 14
                        : 10,

                Foreground =
                    Brushes.White,

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                VerticalAlignment =
                    VerticalAlignment.Center
            };

        return border;
    }

    // =========================================================
    // PIECE GRID
    // =========================================================

    private Grid CreatePieceGrid(
        Piece piece)
    {
        var grid =
            new Grid();

        for (int y = 0;
             y < piece.Height;
             y++)
        {
            grid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                });
        }

        for (int x = 0;
             x < piece.Width;
             x++)
        {
            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        GridLength.Auto
                });
        }

        for (int y = 0;
             y < piece.Height;
             y++)
        {
            for (int x = 0;
                 x < piece.Width;
                 x++)
            {
                bool active =
                    piece.Cells[y, x];

                var border =
                    new Border
                    {
                        Width = 30,

                        Height = 30,

                        BorderBrush =
                            (Brush)FindResource(
                                "DocumentBlueLight"),

                        BorderThickness =
                            new Thickness(1),

                        Background =
                            Brushes.White
                    };

                border.Child =
                    new TextBlock
                    {
                        Text =
                            active
                                ? "X"
                                : ".",

                        FontFamily =
                            new FontFamily(
                                "Consolas"),

                        FontSize = 14,

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Center,

                        Foreground =
                            active
                                ? (Brush)FindResource(
                                    "TextBrush")
                                : (Brush)FindResource(
                                    "MutedBrush")
                    };

                Grid.SetRow(
                    border,
                    y);

                Grid.SetColumn(
                    border,
                    x);

                grid.Children.Add(
                    border);
            }
        }

        return grid;
    }

    // =========================================================
    // COLORS
    // =========================================================

    private Brush GetStateBrush(
        int value,
        int depth)
    {
        return value switch
        {
            0 =>
                (Brush)FindResource(
                    "RedState"),

            1 =>
                (Brush)FindResource(
                    "GreenState"),

            2 =>
                (Brush)FindResource(
                    "BlueState"),

            3 =>
                (Brush)FindResource(
                    "YellowState"),

            _ =>
                Brushes.Gray
        };
    }

    // =========================================================
    // VERIFY
    // =========================================================

    private static void VerifySolution(
        Puzzle puzzle,
        Solution solution)
    {
        if (solution.Positions == null)
        {
            throw new InvalidOperationException(
                "Solver returned no positions.");
        }

        if (solution.Positions.Count !=
            puzzle.Pieces.Count)
        {
            throw new InvalidOperationException(
                "Solver returned an incorrect " +
                "number of positions.");
        }

        if (solution.States == null ||
            solution.States.Count == 0)
        {
            throw new InvalidOperationException(
                "Solver returned no states.");
        }

        int[,] finalState =
            solution.States[
                solution.States.Count - 1];

        if (finalState.GetLength(0) !=
            puzzle.Height ||
            finalState.GetLength(1) !=
            puzzle.Width)
        {
            throw new InvalidOperationException(
                "Final board dimensions are incorrect.");
        }

        for (int y = 0;
             y < puzzle.Height;
             y++)
        {
            for (int x = 0;
                 x < puzzle.Width;
                 x++)
            {
                if (finalState[y, x] != 0)
                {
                    throw new InvalidOperationException(
                        $"Solution is invalid. " +
                        $"Cell ({x},{y}) = " +
                        $"{finalState[y, x]}.");
                }
            }
        }
    }

    // =========================================================
    // SAMPLE CONTEXT
    // =========================================================

    private sealed class SampleContext
    {
        public int SampleNumber { get; init; }

        public required Puzzle Puzzle { get; init; }

        public TextBlock? StatusText { get; init; }

        public TextBlock? CoordinatesText { get; init; }

        public StackPanel? SolutionPanel { get; init; }

        public Button? SolveButton { get; init; }

        public bool IsSolving { get; set; }
    }

    // =========================================================
    // SOLVER RESULT
    // =========================================================

    private sealed class SolverResult
    {
        public Solution? Solution { get; init; }

        public string Diagnostic { get; init; } =
            string.Empty;
    }
}