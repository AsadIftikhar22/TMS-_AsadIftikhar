namespace LightsOut.Wpf.Models;

public sealed class Solution
{
    public Solution(
        IReadOnlyList<Position> positions,
        IReadOnlyList<int[,]> states)
    {
        Positions =
            positions ??
            throw new ArgumentNullException(
                nameof(positions));

        States =
            states ??
            throw new ArgumentNullException(
                nameof(states));
    }

    public IReadOnlyList<Position> Positions { get; }

    public IReadOnlyList<int[,]> States { get; }
}
