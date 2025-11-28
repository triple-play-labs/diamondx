// Game.cs
using System;
using System.Runtime.CompilerServices;
using DiamondX.Core.Models;
using DiamondX.Core.Simulation;
using DiamondX.Core.State;
using SimulationEngine.Random;

[assembly: InternalsVisibleTo("DiamondX.Tests")]

namespace DiamondX.Core;

public class Game
{
    private readonly List<Player> _homeTeam;
    private readonly List<Player> _awayTeam;
    private readonly string _homeTeamName;
    private readonly string _awayTeamName;
    private readonly GameState _state = new();
    private readonly IPlateAppearanceResolver _plateAppearanceResolver;

    private Pitcher _homePitcher;
    private Pitcher _awayPitcher;

    private int _homeBatterIndex;
    private int _awayBatterIndex;

    internal GameState State => _state;
    public string HomeTeamName => _homeTeamName;
    public string AwayTeamName => _awayTeamName;
    public Pitcher HomePitcher => _homePitcher;
    public Pitcher AwayPitcher => _awayPitcher;

    public Game(
        List<Player> homeTeam,
        List<Player> awayTeam,
        string homeTeamName = "Home",
        string awayTeamName = "Away",
        Pitcher? homePitcher = null,
        Pitcher? awayPitcher = null,
        IPlateAppearanceResolver? plateAppearanceResolver = null)
    {
        _homeTeam = homeTeam ?? throw new ArgumentNullException(nameof(homeTeam));
        _awayTeam = awayTeam ?? throw new ArgumentNullException(nameof(awayTeam));
        _homeTeamName = homeTeamName;
        _awayTeamName = awayTeamName;

        if (_homeTeam.Count == 0)
        {
            throw new ArgumentException("Home team must have at least one player.", nameof(homeTeam));
        }

        if (_awayTeam.Count == 0)
        {
            throw new ArgumentException("Away team must have at least one player.", nameof(awayTeam));
        }

        // Create default pitchers if not provided
        _homePitcher = homePitcher ?? CreateDefaultPitcher($"{homeTeamName} Starter");
        _awayPitcher = awayPitcher ?? CreateDefaultPitcher($"{awayTeamName} Starter");

        _plateAppearanceResolver = plateAppearanceResolver ?? new PlateAppearanceResolver(new SystemRandomSource());

        Console.WriteLine($"--- Game Start: {_awayTeamName} @ {_homeTeamName} ---");
    }

    private static Pitcher CreateDefaultPitcher(string name)
    {
        // League average pitcher
        return new Pitcher(
            name: name,
            walkRate: 0.08,
            singlesAllowedRate: 0.15,
            doublesAllowedRate: 0.045,
            triplesAllowedRate: 0.005,
            homeRunsAllowedRate: 0.03,
            strikeoutRate: 0.22,
            fatigueThreshold: 75,
            maxPitchCount: 110
        );
    }

    private AtBatOutcome SimulateAtBat(Player player, Pitcher pitcher)
    {
        Console.WriteLine($"At bat: {player.Name} vs {pitcher.Name} (Pitch #{pitcher.PitchCount + 1}, Fatigue: {pitcher.FatigueLevel:P0})");
        return _plateAppearanceResolver.Resolve(player, pitcher);
    }

    internal void AdvanceRunners(AtBatOutcome outcome, Player batter, bool isHomeTeam)
    {
        if (outcome == AtBatOutcome.Walk)
        {
            HandleWalk(batter, isHomeTeam);
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

        if (basesToAdvance == 0)
        {
            return;
        }

        MoveExistingRunners(basesToAdvance, isHomeTeam);

        if (basesToAdvance < 4)
        {
            _state.SetBase(basesToAdvance - 1, batter);
        }
        else
        {
            _state.AddRun(isHomeTeam);
            Console.WriteLine($"{batter.Name} hits a home run! ({(isHomeTeam ? _homeTeamName : _awayTeamName)} now has {(isHomeTeam ? _state.HomeScore : _state.AwayScore)})");
        }
    }

    private void HandleWalk(Player batter, bool isHomeTeam)
    {
        var first = _state.GetBase(0);
        var second = _state.GetBase(1);
        var third = _state.GetBase(2);

        if (first != null && second != null && third != null)
        {
            _state.AddRun(isHomeTeam);
            Console.WriteLine($"{third.Name} scores! ({(isHomeTeam ? _homeTeamName : _awayTeamName)} now has {(isHomeTeam ? _state.HomeScore : _state.AwayScore)})");
            third = second;
            second = first;
            first = batter;
        }
        else if (first != null && second != null)
        {
            third = second;
            second = first;
            first = batter;
        }
        else if (first != null)
        {
            second = first;
            first = batter;
        }
        else
        {
            first = batter;
        }

        _state.SetBase(0, first);
        _state.SetBase(1, second);
        _state.SetBase(2, third);
    }

    private void MoveExistingRunners(int basesToAdvance, bool isHomeTeam)
    {
        for (int baseIndex = 2; baseIndex >= 0; baseIndex--)
        {
            var runner = _state.GetBase(baseIndex);
            if (runner == null)
            {
                continue;
            }

            _state.SetBase(baseIndex, null);

            int destination = baseIndex + basesToAdvance;
            if (destination >= 3)
            {
                _state.AddRun(isHomeTeam);
                Console.WriteLine($"{runner.Name} scores! ({(isHomeTeam ? _homeTeamName : _awayTeamName)} now has {(isHomeTeam ? _state.HomeScore : _state.AwayScore)})");
            }
            else
            {
                _state.SetBase(destination, runner);
            }
        }
    }

    private void PlayHalfInning(List<Player> battingTeam, bool isHomeTeam)
    {
        int batterIndex = isHomeTeam ? _homeBatterIndex : _awayBatterIndex;
        // Away team bats against home pitcher, home team bats against away pitcher
        Pitcher pitcher = isHomeTeam ? _awayPitcher : _homePitcher;

        while (_state.Outs < 3)
        {
            PrintBases();
            Player currentBatter = battingTeam[batterIndex];
            AtBatOutcome outcome = SimulateAtBat(currentBatter, pitcher);

            switch (outcome)
            {
                case AtBatOutcome.Out:
                    Console.WriteLine("Result: OUT!");
                    _state.RecordOut();
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

            batterIndex = (batterIndex + 1) % battingTeam.Count;
        }

        if (isHomeTeam)
        {
            _homeBatterIndex = batterIndex;
        }
        else
        {
            _awayBatterIndex = batterIndex;
        }

        // Print pitcher status at end of half-inning
        Console.WriteLine($"[{pitcher.Name}: {pitcher.PitchCount} pitches, {pitcher.FatigueLevel:P0} fatigue]");
        Console.WriteLine(new string('-', 20));
    }

    private void PrintBases()
    {
        Console.WriteLine($"Bases: [1B: {_state.Bases[0]?.Name ?? "___"}] [2B: {_state.Bases[1]?.Name ?? "___"}] [3B: {_state.Bases[2]?.Name ?? "___"}]");
    }

    public void PlayGame()
    {
        int inning = 1;

        // Play regulation 9 innings
        while (inning <= 9)
        {
            PlayInning(inning);
            inning++;
        }

        // Extra innings if tied
        while (_state.HomeScore == _state.AwayScore)
        {
            Console.WriteLine($"\n⚾ EXTRA INNINGS! ⚾");
            PlayInning(inning);
            inning++;
        }

        PrintFinalScore(inning - 1);
    }

    private void PlayInning(int inning)
    {
        _state.BeginHalfInning(inning, HalfInning.Top);
        Console.WriteLine($"\nTop of Inning {inning} | Score: {_awayTeamName} {_state.AwayScore} - {_homeTeamName} {_state.HomeScore}");
        PlayHalfInning(_awayTeam, isHomeTeam: false);

        // Walk-off check: if home team is ahead after top of 9th or later, game over
        if (inning >= 9 && _state.HomeScore > _state.AwayScore)
        {
            return;
        }

        _state.BeginHalfInning(inning, HalfInning.Bottom);
        Console.WriteLine($"Bottom of Inning {inning} | Score: {_awayTeamName} {_state.AwayScore} - {_homeTeamName} {_state.HomeScore}");
        PlayHalfInning(_homeTeam, isHomeTeam: true);
    }

    private void PrintFinalScore(int totalInnings)
    {
        Console.WriteLine("\n--- Game Over ---");
        string inningNote = totalInnings > 9 ? $" ({totalInnings} innings)" : "";
        Console.WriteLine($"Final Score: {_awayTeamName} {_state.AwayScore} - {_homeTeamName} {_state.HomeScore}{inningNote}");
        if (_state.HomeScore > _state.AwayScore)
        {
            Console.WriteLine($"{_homeTeamName} Win!");
        }
        else
        {
            Console.WriteLine($"{_awayTeamName} Win!");
        }
    }
}
