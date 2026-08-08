namespace LightsOut.Wpf.Models;

public sealed class PuzzleRecord
{
    public PuzzleRecord(
        int number,
        string input,
        Puzzle puzzle)
    {
        Number = number;
        Input = input;
        Puzzle = puzzle;
    }

    public int Number { get; }

    public string Input { get; }

    public Puzzle Puzzle { get; }

    public Solution? Solution { get; set; }

    public string Status { get; set; } = "Waiting";

    public string Diagnostic { get; set; } = string.Empty;
}