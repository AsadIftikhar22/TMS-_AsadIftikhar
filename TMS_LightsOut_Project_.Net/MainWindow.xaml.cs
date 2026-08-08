using LightsOut.Wpf.Models;
using LightsOut.Wpf.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LightsOut.Wpf;

public partial class MainWindow : Window
{
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
        LoadSamplesFromFolder();
    }

    // =========================================================
    // LOAD SAMPLES FROM /Samples
    // =========================================================

    private void LoadSamplesFromFolder()
    {
        SamplesPanel.Children.Clear();

        try
        {
            string samplesFolder =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Samples");

            if (!Directory.Exists(samplesFolder))
            {
                OverallStatusText.Text =
                    "Samples folder not found.";

                SamplesPanel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"Create a Samples folder here:\n\n" +
                            $"{samplesFolder}\n\n" +
                            "Put one puzzle in each .txt file.",

                        Foreground =
                            Brushes.Firebrick,

                        FontSize = 15,

                        TextWrapping =
                            TextWrapping.Wrap,

                        Margin =
                            new Thickness(10)
                    });

                return;
            }

            string[] files =
                Directory
                    .GetFiles(
                        samplesFolder,
                        "*.txt",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(
                        x => x,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            if (files.Length == 0)
            {
                OverallStatusText.Text =
                    "No sample .txt files found.";

                SamplesPanel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"No .txt files were found in:\n\n" +
                            samplesFolder,

                        Foreground =
                            Brushes.Firebrick,

                        FontSize = 15,

                        TextWrapping =
                            TextWrapping.Wrap,

                        Margin =
                            new Thickness(10)
                    });

                return;
            }

            int loadedCount = 0;

            var errors =
                new List<string>();

            for (int i = 0;
                 i < files.Length;
                 i++)
            {
                string file =
                    files[i];

                try
                {
                    string text =
                        File.ReadAllText(file);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        errors.Add(
                            $"{Path.GetFileName(file)}: file is empty.");

                        continue;
                    }

                    Puzzle puzzle =
                        PuzzleParser.Parse(text);

                    loadedCount++;

                    AddSampleCard(
                        loadedCount,
                        Path.GetFileName(file),
                        puzzle);
                }
                catch (Exception ex)
                {
                    errors.Add(
                        $"{Path.GetFileName(file)}: {ex.Message}");
                }
            }

            if (errors.Count == 0)
            {
                OverallStatusText.Text =
                    $"{loadedCount} sample(s) loaded from Samples folder.";
            }
            else
            {
                OverallStatusText.Text =
                    $"{loadedCount} sample(s) loaded. " +
                    $"{errors.Count} file(s) could not be loaded.";

                var errorPanel =
                    new Border
                    {
                        Background =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    255,
                                    245,
                                    245)),

                        BorderBrush =
                            Brushes.Firebrick,

                        BorderThickness =
                            new Thickness(1),

                        CornerRadius =
                            new CornerRadius(6),

                        Padding =
                            new Thickness(12),

                        Margin =
                            new Thickness(
                                0,
                                0,
                                0,
                                15)
                    };

                var errorStack =
                    new StackPanel();

                errorStack.Children.Add(
                    new TextBlock
                    {
                        Text =
                            "Some sample files could not be loaded:",

                        FontWeight =
                            FontWeights.Bold,

                        Foreground =
                            Brushes.Firebrick,

                        Margin =
                            new Thickness(
                                0,
                                0,
                                0,
                                5)
                    });

                foreach (string error in errors)
                {
                    errorStack.Children.Add(
                        new TextBlock
                        {
                            Text =
                                "• " + error,

                            Foreground =
                                Brushes.Firebrick,

                            TextWrapping =
                                TextWrapping.Wrap,

                            Margin =
                                new Thickness(
                                    0,
                                    2,
                                    0,
                                    2)
                        });
                }

                errorPanel.Child =
                    errorStack;

                SamplesPanel.Children.Insert(
                    0,
                    errorPanel);
            }
        }
        catch (Exception ex)
        {
            OverallStatusText.Text =
                "Failed to load samples.";

            SamplesPanel.Children.Add(
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
                });
        }
    }

    // =========================================================
    // SAMPLE CARD
    // =========================================================

    private void AddSampleCard(
        int sampleNumber,
        string fileName,
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
                    new GridLength(
                        1,
                        GridUnitType.Star)
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
                    fileName,

                Margin =
                    new Thickness(
                        0,
                        3,
                        0,
                        0),

                FontFamily =
                    new FontFamily(
                        "Consolas"),

                FontSize = 13,

                Foreground =
                    (Brush)FindResource(
                        "DocumentBlueLight")
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
        // SOLVE BUTTON
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

                FontSize = 15
            };

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
        // PROBLEM
        // =====================================================

        mainPanel.Children.Add(
            CreateSectionTitle(
                "Problem"));

        var problemPanel =
            new StackPanel
            {
                Orientation =
                    Orientation.Vertical
            };

        // =====================================================
        // INITIAL BOARD
        // =====================================================

        var boardPanel =
            new StackPanel
            {
                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        15)
            };

        boardPanel.Children.Add(
            new TextBlock
            {
                Text =
                    "Initial Board",

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

        problemPanel.Children.Add(
            boardPanel);

        // =====================================================
        // PIECES
        // =====================================================

        problemPanel.Children.Add(
            new TextBlock
            {
                Text =
                    "Pieces",

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

        var piecesPanel =
            new WrapPanel
            {
                Orientation =
                    Orientation.Horizontal,

                HorizontalAlignment =
                    HorizontalAlignment.Left
            };

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
                            14,
                            12)
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

        problemPanel.Children.Add(
            piecesPanel);

        mainPanel.Children.Add(
            problemPanel);

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
                    ScrollBarVisibility.Disabled,

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
            new WrapPanel
            {
                Orientation =
                    Orientation.Horizontal,

                HorizontalAlignment =
                    HorizontalAlignment.Left
            };

        solutionScroll.Content =
            solutionPanel;

        mainPanel.Children.Add(
            solutionScroll);

        // =====================================================
        // CONTEXT
        // =====================================================

        var context =
            new SampleContext
            {
                SampleNumber =
                    sampleNumber,

                FileName =
                    fileName,

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

        solveButton.Click +=
            SolveSample_Click;

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
            Text =
                text,

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
    // SOLVE SAMPLE
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

            // =================================================
            // NO SOLUTION
            // =================================================

            if (result.Solution == null)
            {
                context.StatusText.Text =
                    "No solution found.";

                context.StatusText.Foreground =
                    Brushes.Firebrick;

                MessageBox.Show(
                    this,
                    $"{context.FileName}\n\n" +
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
                $"{context.FileName} solved.";
        }
        catch (Exception ex)
        {
            context.StatusText!.Text =
                "Error while solving.";

            context.StatusText.Foreground =
                Brushes.Firebrick;

            MessageBox.Show(
                this,
                $"{context.FileName}\n\n" +
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
                        Text =
                            "→",

                        FontSize = 30,

                        Foreground =
                            (Brush)FindResource(
                                "DocumentBlueLight"),

                        VerticalAlignment =
                            VerticalAlignment.Center,

                        Margin =
                            new Thickness(
                                8,
                                0,
                                8,
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
                    Text =
                        caption,

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
                Width =
                    size,

                Height =
                    size,

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
                        Width =
                            30,

                        Height =
                            30,

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

        public string FileName { get; init; } =
            string.Empty;

        public required Puzzle Puzzle { get; init; }

        public TextBlock? StatusText { get; init; }

        public TextBlock? CoordinatesText { get; init; }

        public Panel? SolutionPanel { get; init; }

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