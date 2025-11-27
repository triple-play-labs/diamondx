// Game.cs

namespace DiamondX;

public class Game
{
    private readonly List<Player> _homeTeam;
    private readonly List<Player> _awayTeam;
    private int _homeScore;
    private int _awayScore;
    private int _inning;
    private int _outs;
    private int _homeBatterIndex;
    private int _awayBatterIndex;
    private readonly Random _random = new();

    private readonly Player?[] _bases = new Player?[3];

    public Game(List<Player> homeTeam, List<Player> awayTeam)
    {
        _homeTeam = homeTeam;
        _awayTeam = awayTeam;
        Console.WriteLine("--- Game Start ---");
    }

    private AtBatOutcome SimulateAtBat(Player player)
    {
        Console.WriteLine($"At bat: {player.Name}");
        double roll = _random.NextDouble();

        // We check outcomes in a cascading manner.
        // The order doesn't strictly matter, but it's logical to check from lowest to highest probability.
        if (roll < player.WalkRate) return AtBatOutcome.Walk;
        roll -= player.WalkRate;

        if (roll < player.SingleRate) return AtBatOutcome.Single;
        roll -= player.SingleRate;

        if (roll < player.DoubleRate) return AtBatOutcome.Double;
        roll -= player.DoubleRate;

        if (roll < player.TripleRate) return AtBatOutcome.Triple;
        roll -= player.TripleRate;

        if (roll < player.HomeRunRate) return AtBatOutcome.HomeRun;

        return AtBatOutcome.Out; // If no other outcome was hit, it's an out.
    }

    private void AdvanceRunners(AtBatOutcome outcome, Player batter, bool isHomeTeam)
    {
        if (outcome == AtBatOutcome.Walk)
        {
            // Handle walk: advance runners only if forced
            if (_bases[0] != null && _bases[1] != null && _bases[2] != null) { // Bases loaded
                if (isHomeTeam) _homeScore++; else _awayScore++;
                Console.WriteLine($"{_bases[2]?.Name} scores!");
                _bases[2] = _bases[1];
                _bases[1] = _bases[0];
                _bases[0] = batter;
            } else if (_bases[0] != null && _bases[1] != null) { // 1st and 2nd occupied
                _bases[2] = _bases[1];
                _bases[1] = _bases[0];
                _bases[0] = batter;
            } else if (_bases[0] != null) { // 1st occupied
                _bases[1] = _bases[0];
                _bases[0] = batter;
            } else { // Bases empty or only runners on 2nd/3rd
                _bases[0] = batter;
            }
            return;
        }

        int basesToAdvance = outcome switch
        {
            AtBatOutcome.Single => 1,
            AtBatOutcome.Double => 2,
            AtBatOutcome.Triple => 3,
            AtBatOutcome.HomeRun => 4,
            _ => 0
        };

        if (basesToAdvance == 0) return;

        // Move runners on base
        for (int i = 2; i >= 0; i--)
        {
            if (_bases[i] != null)
            {
                int newBaseIndex = i + basesToAdvance;
                if (newBaseIndex >= 3)
                {
                    if (isHomeTeam) _homeScore++; else _awayScore++;
                    Console.WriteLine($"{_bases[i]?.Name} scores!");
                    _bases[i] = null;
                }
                else
                {
                    _bases[newBaseIndex] = _bases[i];
                    _bases[i] = null;
                }
            }
        }

        // Place batter
        if (basesToAdvance < 4)
        {
            _bases[basesToAdvance - 1] = batter;
        }
        else // Home Run
        {
            if (isHomeTeam) _homeScore++; else _awayScore++;
            Console.WriteLine($"{batter.Name} hits a home run! A run scores!");
        }
    }


    private void PlayHalfInning(List<Player> team, bool isHomeTeam)
    {
        _outs = 0;
        Array.Clear(_bases, 0, _bases.Length); // Clear bases at the start of the inning
        // Use a reference to the correct batter index for the current team
        ref int batterIndex = ref isHomeTeam ? ref _homeBatterIndex : ref _awayBatterIndex;

        while (_outs < 3)
        {
            PrintBases(); // Show the state of the bases
            Player currentBatter = team[batterIndex];
            AtBatOutcome outcome = SimulateAtBat(currentBatter);

            // Handle the outcome
            switch (outcome)
            {
                case AtBatOutcome.Out:
                    Console.WriteLine("Result: OUT!");
                    _outs++;
                    break;
                case AtBatOutcome.Walk:
                    Console.WriteLine("Result: WALK!");
                    AdvanceRunners(outcome, currentBatter, isHomeTeam);
                    break;
                default:
                    Console.WriteLine($"Result: {outcome.ToString().ToUpper()}!");
                    AdvanceRunners(outcome, currentBatter, isHomeTeam);
                    break;
            }

            batterIndex = (batterIndex + 1) % team.Count; // Move to the next batter
        }
        Console.WriteLine(new string('-', 20));
    }

    private void PrintBases()
    {
        // Simple console representation of the bases.
        Console.WriteLine($"Bases: [1B: {_bases[0]?.Name ?? "___"}] [2B: {_bases[1]?.Name ?? "___"}] [3B: {_bases[2]?.Name ?? "___"}]");
    }

    public void PlayGame()
    {
        for (_inning = 1; _inning <= 9; _inning++)
        {
            // Away team bats
            Console.WriteLine($"\nTop of Inning {_inning} | Score: Away {_awayScore} - Home {_homeScore}");
            PlayHalfInning(_awayTeam, isHomeTeam: false);

            // Home team bats, but only if they are not winning in the bottom of the 9th
            if (_inning == 9 && _homeScore > _awayScore)
            {
                break; // Walk-off win, no need for home team to bat.
            }

            Console.WriteLine($"Bottom of Inning {_inning} | Score: Away {_awayScore} - Home {_homeScore}");
            PlayHalfInning(_homeTeam, isHomeTeam: true);
        }

        PrintFinalScore();
    }



    private void PrintFinalScore()
    {
        Console.WriteLine("\n--- Game Over ---");
        Console.WriteLine($"Final Score: Away {_awayScore} - Home {_homeScore}");
        if (_homeScore > _awayScore)
        {
            Console.WriteLine("Home Team Wins!");
        }
        else if (_awayScore > _homeScore)
        {
            Console.WriteLine("Away Team Wins!");
        }
        else
        {
            Console.WriteLine("It's a tie!"); // We'll handle extra innings later.
        }
    }
}
