using LightsOut.Wpf.Models;
using LightsOut.Wpf.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LightsOut.Wpf;

public partial class MainWindow : Window
{
    // =========================================================
    // FIELDS
    // =========================================================

    /*
     * Keeps track of how many samples are currently being solved.
     *
     * This is better than a simple bool because multiple sample
     * Solve buttons can potentially be running at the same time.
     */
    private int _solvingCount;

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
    // RESET ALL
    // =========================================================

    private void ResetAll_Click(
        object sender,
        RoutedEventArgs e)
    {
        /*
         * Do not rebuild the UI while one or more background
         * solver operations are still running.
         *
         * Otherwise a running solver could finish and try to
         * update a SampleContext that has already been removed.
         */
        if (_solvingCount > 0)
        {
            MessageBox.Show(
                this,
                "Please wait until all solving operations have finished.",
                "Reset All",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        /*
         * Clear the current sample cards.
         *
         * This removes:
         * - Existing solution boards
         * - Coordinates
         * - Status messages
         * - Existing Solve buttons
         * - Existing SampleContext objects
         */
        SamplesPanel.Children.Clear();

        /*
         * Restore the overall application status while
         * the samples are being loaded again.
         */
        OverallStatusText.Text =
            "Loading samples...";

        /*
         * Read all TXT files again from the Samples folder.
         *
         * Every TXT file is loaded as an independent puzzle.
         */
        LoadSamplesFromFolder();
    }

    // =========================================================
    // FIND SAMPLES FOLDER
    // =========================================================

    private static string? FindSamplesFolder()
    {
        /*
         * First look beside the executable.
         *
         * This is used when the application is published
         * or when Samples has been copied to the output folder.
         */

        string applicationFolder =
            AppContext.BaseDirectory;

        string applicationSamples =
            Path.Combine(
                applicationFolder,
                "Samples");

        if (Directory.Exists(applicationSamples))
        {
            return applicationSamples;
        }

        /*
         * During development the project folder may be:
         *
         * Project/
         *     Samples/
         *
         * while the executable is:
         *
         * Project/
         *     bin/
         *         Debug/
         *             net8.0-windows/
         *
         * Walk upward until Samples is found.
         */

        DirectoryInfo? current =
            new DirectoryInfo(
                applicationFolder);

        while (current != null)
        {
            string candidate =
                Path.Combine(
                    current.FullName,
                    "Samples");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current =
                current.Parent;
        }

        /*
         * Also check current working directory.
         */

        string workingSamples =
            Path.Combine(
                Environment.CurrentDirectory,
                "Samples");

        if (Directory.Exists(workingSamples))
        {
            return workingSamples;
        }

        return null;
    }

    // =========================================================
    // LOAD ALL SAMPLE TXT FILES
    // =========================================================

    private void LoadSamplesFromFolder()
    {
        SamplesPanel.Children.Clear();

        try
        {
            string? samplesFolder =
                FindSamplesFolder();

            if (string.IsNullOrWhiteSpace(samplesFolder))
            {
                OverallStatusText.Text =
                    "Samples folder was not found.";

                SamplesPanel.Children.Add(
                    CreateErrorText(
                        "Samples folder was not found.\n\n" +
                        "Create this folder in your project:\n\n" +
                        "Samples\\\n" +
                        "    Sample1.txt\n" +
                        "    Sample2.txt\n" +
                        "    Sample3.txt\n\n" +
                        "Each TXT file must contain one puzzle."
                    ));

                return;
            }

            string[] files =
                Directory.GetFiles(
                    samplesFolder,
                    "*.txt",
                    SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                OverallStatusText.Text =
                    "No TXT sample files found.";

                SamplesPanel.Children.Add(
                    CreateErrorText(
                        $"No .txt files were found in:\n\n" +
                        $"{samplesFolder}"));

                return;
            }

            /*
             * Sort naturally by filename.
             *
             * Sample1.txt
             * Sample2.txt
             * Sample3.txt
             */

            Array.Sort(
                files,
                StringComparer.OrdinalIgnoreCase);

            int sampleNumber = 0;

            int loaded = 0;

            foreach (string file in files)
            {
                try
                {
                    string text =
                        File.ReadAllText(file);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    IReadOnlyList<Puzzle> puzzles =
                        PuzzleParser.ParseMany(text);

                    /*
                     * Normally one TXT file contains one puzzle.
                     *
                     * If a file contains multiple puzzles,
                     * they will still be displayed individually.
                     */

                    foreach (Puzzle puzzle in puzzles)
                    {
                        sampleNumber++;

                        AddSampleCard(
                            sampleNumber,
                            puzzle,
                            Path.GetFileName(file));

                        loaded++;
                    }
                }
                catch (Exception ex)
                {
                    sampleNumber++;

                    AddFileErrorCard(
                        sampleNumber,
                        Path.GetFileName(file),
                        ex);
                }
            }

            OverallStatusText.Text =
                $"{loaded} sample(s) loaded from Samples folder.";
        }
        catch (Exception ex)
        {
            OverallStatusText.Text =
                "Failed to load samples.";

            SamplesPanel.Children.Add(
                CreateErrorText(
                    ex.ToString()));
        }
    }

    // =========================================================
    // ERROR TEXT
    // =========================================================

    private TextBlock CreateErrorText(
        string text)
    {
        return new TextBlock
        {
            Text = text,

            Foreground =
                Brushes.Firebrick,

            TextWrapping =
                TextWrapping.Wrap,

            Margin =
                new Thickness(10),

            FontSize = 14
        };
    }

    // =========================================================
    // FILE ERROR CARD
    // =========================================================

    private void AddFileErrorCard(
        int sampleNumber,
        string fileName,
        Exception exception)
    {
        var border =
            new Border
            {
                Background =
                    Brushes.White,

                BorderBrush =
                    Brushes.Firebrick,

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
                        15)
            };

        border.Child =
            new TextBlock
            {
                Text =
                    $"Sample {sampleNumber}\n\n" +
                    $"File: {fileName}\n\n" +
                    $"Error:\n{exception.Message}",

                Foreground =
                    Brushes.Firebrick,

                TextWrapping =
                    TextWrapping.Wrap
            };

        SamplesPanel.Children.Add(
            border);
    }

    // =========================================================
    // SAMPLE CARD
    // =========================================================

    private void AddSampleCard(
        int sampleNumber,
        Puzzle puzzle,
        string fileName)
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
                        18)
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
                    $"File: {fileName}",

                Margin =
                    new Thickness(
                        0,
                        4,
                        0,
                        0),

                Foreground =
                    (Brush)FindResource(
                        "MutedBrush"),

                FontSize = 13
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
                        4,
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

        problemPanel.Children.Add(
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

        problemPanel.Children.Add(
            CreateBoardDiagram(
                puzzle.InitialBoard,
                puzzle.Depth,
                false));

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
                        15,
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
                            12,
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
                            4,
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

        /*
         * No horizontal ScrollViewer.
         *
         * The solution uses WrapPanel so boards automatically
         * move to the next row.
         */

        var solutionPanel =
            new WrapPanel
            {
                Orientation =
                    Orientation.Horizontal,

                HorizontalAlignment =
                    HorizontalAlignment.Left
            };

        mainPanel.Children.Add(
            solutionPanel);

        // =====================================================
        // CONTEXT
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

        solveButton.Click +=
            SolveSample_Click;

        // =====================================================
        // ADD
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
    // SOLVE
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

        /*
         * Increase the number of active solver operations.
         *
         * Reset All uses this value to know whether it is safe
         * to rebuild the sample UI.
         */
        _solvingCount++;

        button.IsEnabled = false;

        context.StatusText!.Text =
            "Solving Please wait....";

        context.StatusText.Foreground =
            (Brush)FindResource(
                "DocumentBlueLight");

        context.StatusText.FontSize = 20;

        context.StatusText.FontWeight =
            FontWeights.Bold;

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

            /*
             * Decrease the active solver count.
             *
             * Math.Max prevents the value from ever becoming
             * negative if something unexpected happens.
             */
            _solvingCount =
                Math.Max(
                    0,
                    _solvingCount - 1);
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

                        FontSize = 24,

                        Foreground =
                            (Brush)FindResource(
                                "DocumentBlueLight"),

                        VerticalAlignment =
                            VerticalAlignment.Center,

                        Margin =
                            new Thickness(
                                6,
                                0,
                                6,
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

            /*
             * Keep the animation short.
             */
            await Task.Delay(80);
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
                        4,
                        8)
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
                    new Thickness(4),

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
                            4,
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
                "number of solution positions.");
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
    // SLOW SCROLL
    // =========================================================

    private void SamplesScrollViewer_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        // Smaller value = slower scrolling.
        const double scrollAmount = 30;

        double offset =
            scrollViewer.VerticalOffset;

        if (e.Delta < 0)
        {
            scrollViewer.ScrollToVerticalOffset(
                offset + scrollAmount);
        }
        else
        {
            scrollViewer.ScrollToVerticalOffset(
                offset - scrollAmount);
        }

        e.Handled = true;
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

        public WrapPanel? SolutionPanel { get; init; }

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