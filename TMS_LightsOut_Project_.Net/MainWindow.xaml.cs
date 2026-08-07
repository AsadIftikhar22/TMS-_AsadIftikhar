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
    private readonly string _sampleInput =
        """
        2
        001,011,011
        .X,XX XX .X,.X,XX
        """;

    private Puzzle? _currentPuzzle;
    private bool _isAnimating;

    public MainWindow()
    {
        InitializeComponent();

        InputTextBox.Text = _sampleInput.Trim();
        RenderFromInput();
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsInitialized || _isAnimating)
            return;

        RenderFromInput(false);
    }

    private void LoadInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Lights Out input",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            InputTextBox.Text = File.ReadAllText(dialog.FileName);
            RenderFromInput();
            SetStatus("Input loaded.", false);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_isAnimating)
            return;

        InputTextBox.Text = _sampleInput.Trim();
        DepthComboBox.SelectedIndex = 0;
        CoordinatesText.Text = string.Empty;
        SolutionStepsPanel.Children.Clear();
        SetStatus("Ready.", false);
        RenderFromInput();
    }

    private async void Solve_Click(object sender, RoutedEventArgs e)
    {
        if (_isAnimating)
            return;

        try
        {
            _currentPuzzle = PuzzleParser.Parse(InputTextBox.Text);

            int depth = _currentPuzzle.Depth;
            DepthComboBox.SelectedIndex = depth - 2;

            SetStatus("Searching for a solution…", false);
            CoordinatesText.Text = string.Empty;
            SolutionStepsPanel.Children.Clear();

            _isAnimating = true;
            SetButtonsEnabled(false);

            // Keep the UI responsive while the search runs.
            Solution? solution = await Task.Run(() =>
                new LightsOutSolver(_currentPuzzle).Solve());

            if (solution is null)
            {
                SetStatus("No solution was found for this input.", true);
                return;
            }

            CoordinatesText.Text =
                "Solution: " +
                string.Join(
                    "   ",
                    solution.Positions.Select(p => $"({p.X},{p.Y})"));

            await AnimateSolution(solution);

            SetStatus(
                "Solved! All cells have the final state 0.",
                false);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _isAnimating = false;
            SetButtonsEnabled(true);
        }
    }

    private async Task AnimateSolution(Solution solution)
    {
        SolutionStepsPanel.Children.Clear();

        for (int i = 0; i < solution.States.Count; i++)
        {
            int[,] state = solution.States[i];

            if (i > 0)
            {
                var arrow = new TextBlock
                {
                    Text = "→",
                    FontSize = 35,
                    Foreground = (Brush)FindResource("DocumentBlueLight"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(14, 0, 14, 0)
                };

                SolutionStepsPanel.Children.Add(arrow);
            }

            string caption = i == 0
                ? "Initial board"
                : $"Piece {i}: ({solution.Positions[i - 1].X},{solution.Positions[i - 1].Y})";

            SolutionStepsPanel.Children.Add(
                CreateBoardDiagram(state, _currentPuzzle!.Depth, caption));

            await Task.Delay(550);
        }
    }

    private void RenderFromInput(bool clearStatus = true)
    {
        try
        {
            Puzzle puzzle = PuzzleParser.Parse(InputTextBox.Text);
            _currentPuzzle = puzzle;

            DepthComboBox.SelectedIndex = puzzle.Depth - 2;

            RenderBoard(puzzle.InitialBoard, puzzle.Depth);
            RenderPieces(puzzle.Pieces);

            if (clearStatus)
                SetStatus("Ready.", false);

            InputStatusText.Text =
                $"{puzzle.Height} × {puzzle.Width} board · {puzzle.Pieces.Count} piece(s)";
        }
        catch
        {
            BoardGrid.Children.Clear();
            PiecesPanel.Children.Clear();
            InputStatusText.Text = "Enter a valid three-line puzzle.";
        }
    }

    private void RenderBoard(int[,] board, int depth)
    {
        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();

        int height = board.GetLength(0);
        int width = board.GetLength(1);

        for (int y = 0; y < height; y++)
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int x = 0; x < width; x++)
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Border cell = CreateCell(
                    board[y, x],
                    depth,
                    42);

                Grid.SetRow(cell, y);
                Grid.SetColumn(cell, x);
                BoardGrid.Children.Add(cell);
            }
        }
    }

    private void RenderPieces(IReadOnlyList<Piece> pieces)
    {
        PiecesPanel.Children.Clear();

        for (int i = 0; i < pieces.Count; i++)
        {
            StackPanel wrapper = new()
            {
                Margin = new Thickness(0, 0, 22, 0)
            };

            Grid grid = CreatePieceGrid(pieces[i]);

            wrapper.Children.Add(grid);

            TextBlock label = new()
            {
                Text = $"piece {i + 1}",
                FontFamily = new FontFamily("Georgia"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 7, 0, 0)
            };

            wrapper.Children.Add(label);

            PiecesPanel.Children.Add(wrapper);
        }
    }

    private Grid CreatePieceGrid(Piece piece)
    {
        Grid grid = new();

        for (int y = 0; y < piece.Height; y++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int x = 0; x < piece.Width; x++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        for (int y = 0; y < piece.Height; y++)
        {
            for (int x = 0; x < piece.Width; x++)
            {
                Border border = new()
                {
                    Width = 34,
                    Height = 34,
                    BorderBrush = (Brush)FindResource("DocumentBlueLight"),
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White
                };

                if (piece.Cells[y, x])
                {
                    TextBlock text = new()
                    {
                        Text = "X",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = (Brush)FindResource("TextBrush")
                    };

                    border.Child = text;
                }
                else
                {
                    TextBlock text = new()
                    {
                        Text = ".",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = (Brush)FindResource("MutedBrush")
                    };

                    border.Child = text;
                }

                Grid.SetRow(border, y);
                Grid.SetColumn(border, x);
                grid.Children.Add(border);
            }
        }

        return grid;
    }

    private Border CreateCell(int value, int depth, double size)
    {
        var border = new Border
        {
            Width = size,
            Height = size,
            BorderBrush = (Brush)FindResource("DocumentBlueLight"),
            BorderThickness = new Thickness(1),
            Background = GetStateBrush(value, depth),
            CornerRadius = new CornerRadius(2)
        };

        var text = new TextBlock
        {
            Text = value.ToString(),
            FontFamily = new FontFamily("Consolas"),
            FontSize = size >= 40 ? 15 : 12,
            Foreground = value == 0 ? Brushes.White : Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        border.Child = text;

        return border;
    }

    private FrameworkElement CreateBoardDiagram(
        int[,] board,
        int depth,
        string caption)
    {
        var wrapper = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Top
        };

        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };

        int height = board.GetLength(0);
        int width = board.GetLength(1);

        for (int y = 0; y < height; y++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int x = 0; x < width; x++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Border cell = CreateCell(board[y, x], depth, 38);
                Grid.SetRow(cell, y);
                Grid.SetColumn(cell, x);
                grid.Children.Add(cell);
            }
        }

        var frame = new Border
        {
            BorderBrush = (Brush)FindResource("DocumentBlueLight"),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(6),
            Child = grid
        };

        wrapper.Children.Add(frame);

        wrapper.Children.Add(new TextBlock
        {
            Text = caption,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });

        return wrapper;
    }

    private Brush GetStateBrush(int value, int depth)
    {
        return value switch
        {
            0 => (Brush)FindResource("RedState"),
            1 => (Brush)FindResource("GreenState"),
            2 => (Brush)FindResource("BlueState"),
            3 => (Brush)FindResource("YellowState"),
            _ => Brushes.Gray
        };
    }

    private void SetStatus(string message, bool error)
    {
        StatusText.Text = message;
        StatusText.Foreground = error
            ? Brushes.Firebrick
            : (Brush)FindResource("MutedBrush");
    }

    private void SetButtonsEnabled(bool enabled)
    {
        foreach (Button button in FindVisualChildren<Button>(this))
        {
            button.IsEnabled = enabled;
        }
    }

    private void ShowError(string message)
    {
        MessageBox.Show(
            this,
            message,
            "Lights Out",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        SetStatus(message, true);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject)
        where T : DependencyObject
    {
        if (dependencyObject == null)
            yield break;

        for (int i = 0;
             i < VisualTreeHelper.GetChildrenCount(dependencyObject);
             i++)
        {
            DependencyObject child =
                VisualTreeHelper.GetChild(dependencyObject, i);

            if (child is T typedChild)
                yield return typedChild;

            foreach (T descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }
}
