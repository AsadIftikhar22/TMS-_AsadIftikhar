using LightsOut.Wpf.Models;

namespace LightsOut.Wpf.Services;

public static class SamplePuzzleRepository
{
    public static IReadOnlyList<PuzzleRecord> LoadSamples()
    {
        var samples =
            new[]
            {
                Sample1,
                Sample2,
                Sample3,
                Sample4,
                Sample5,
                Sample6,
                Sample7,
                Sample8,
                Sample9,
                Sample10
            };

        var result =
            new List<PuzzleRecord>();

        for (int i = 0;
             i < samples.Length;
             i++)
        {
            string input =
                samples[i].Trim();

            Puzzle puzzle =
                PuzzleParser.Parse(input);

            result.Add(
                new PuzzleRecord(
                    i + 1,
                    input,
                    puzzle));
        }

        return result;
    }

    // =========================================================
    // SAMPLE 1
    // =========================================================

    private const string Sample1 =
"""
2
100,101,011
..X,XXX,X.. X,X,X .X,XX XX.,.X.,.XX XX,X. XX .XX,XX.
""";

    // =========================================================
    // SAMPLE 2
    // =========================================================

    private const string Sample2 =
"""
4
3302012,3221112,3121312,1312033,0201003,0101102,0221020,0302223,0000000
.X,XX,X.,X. .XXXX,.XXXX,XXXXX,..X.. XX..,XXXX,XXX.,XXXX X.,X.,X.,XX .X...,XXXX.,...X.,...XX,...X. X..,X..,X.X,XXX,XXX ..XX,.XXX,.X.X,.X..,XX.. XXX,XX. XXX.,.XXX,.XXX,.XXX,...X .XX.X,.XXXX,XXXXX,..X.. ..XX,..XX,.XX.,.XX.,XXX. .XXX,XX.. ..X..,..XXX,..XXX,XXXX.,..X.. X.X..,XXX..,XXX..,XXXXX,X.X.. XX..,.XXX .XX..,XXXX.,.XXX.,.XXXX,.X... XXXXX,....X
""";

    // =========================================================
    // SAMPLE 3
    // =========================================================

    private const string Sample3 =
"""
2
10010010,00101010,01111100,11111110,10101001,01001000
.X,XX,XX,X. .XX,XX. XXXXX,XXXX.,.XXX.,..XX.,..X.. X,X,X,X,X ..X..,XXXXX ..XX.,..XXX,XXXX.,XXXXX,X.... .X,XX ...X.,..XXX,XXXXX,.XX..,..X.. XXX.,.XXX,.XXX,XX.X,X... XX,X. XXX..,.XX..,..X..,.XX..,..XXX X.XXX,XXXX.,.XXXX,XXX.. XX,X.,X.,XX,.X X,X,X ..X..,XXXXX,..X.. XXXXX,XXX.X,XX...
""";

    // =========================================================
    // SAMPLE 4
    // =========================================================

    private const string Sample4 =
"""
2
100000,011101,101100,110000,011000
.X,XX,X. XXX XXX,XX. .X.,.X.,.X.,XXX,.X. X.,XX,XX .X.,XXX,X.. X...,XXXX X..,X..,XXX,.X. XXX,.X.,.X. .X.,.X.,XX.,.XX .X,XX,X. X.XXX,XXX.. XX.,.X.,.X.,.XX .X,XX,.X X.,X.,XX
""";

    // =========================================================
    // SAMPLE 5
    // =========================================================

    private const string Sample5 =
"""
4
132330,230323,301031,223121,332313
.X,.X,XX,X.,X. ..XX,XXXX,..X. XX.,.XX .XX,..X,XXX,X.. .X.,.X.,XXX,..X .X,.X,.X,XX,X. XX..,.XXX XX,.X,XX XX,XX X...,XXXX,...X,...X .X,.X,XX,X. .X,.X,XX XX,XX,.X XXX,..X,..X,..X
""";

    // =========================================================
    // SAMPLE 6
    // =========================================================

    private const string Sample6 =
"""
4
01230,00130,33203,02131,23313,03010,33320
XXX.,.X..,.XXX,..X.,..X. X.XX.,XX.XX,XXXX. XX.XX,XXXX.,..XX. ...XX,...XX,...X.,XXXX. .XX.,.XXX,XXX.,.X.. XXXX,...X .X,XX,X. .X,XX,X. .X..,XX..,XXXX,.X.. .XX,.X.,XXX,XXX,..X .X.,.XX,.XX,XX.,.X. .X..,XX..,.XXX
""";

    // =========================================================
    // SAMPLE 7
    // =========================================================

    private const string Sample7 =
"""
3
2121,2212,1001,2011,1211,2111
X...,X.X.,XXXX,XX.. XXX,X.X XX,X. XX,.X,.X XX.,.XX,.X.,.X. XX..,.XXX .X.,.X.,XX.,XXX ..XX,..XX,XXX. .X..,.XX.,X.X.,XXXX X,X,X,X .X.,.XX,.X.,.X.,XXX
""";

    // =========================================================
    // SAMPLE 8
    // =========================================================

    private const string Sample8 =
"""
3
110102,001200,110221,020120
..X,..X,..X,XXX ..X,XXX,..X XXXXX,X...X XX,.X,.X,.X XXX .X,.X,XX XXX,X.. XX.,.XX .X.,XX.,.XX,XX. X..,XXX,..X XX,X.,XX ..X..,XXXXX
""";

    // =========================================================
    // SAMPLE 9
    // =========================================================

    private const string Sample9 =
"""
3
102020,120110,001002,100222,112022
.X..,.XX.,XXX.,..X.,..XX ..X,XXX,..X ..X,.XX,XX.,X..,X.. X..,XX.,XXX,XXX,X.. .X...,XXXXX,...X.,...X.,...X. XX.,.XX,.XX,.X.,XX. .XX.,.XX.,XXX.,..XX X.X.,XXXX,X... X.,X.,XX,XX .X.,XXX,.XX,.X. .X,XX,XX
""";

    // =========================================================
    // SAMPLE 10
    // =========================================================

    private const string Sample10 =
"""
2
0100,0110,1010,1110
X.,XX,XX X...,XXXX XXX X,X XX XX,XX,.X,.X ..XX,XXX.
""";
}