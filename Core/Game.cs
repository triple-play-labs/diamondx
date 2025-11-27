// Game.cs
using System;
using System.Runtime.CompilerServices;
using DiamondX.Core.Models;
using DiamondX.Core.Random;
using DiamondX.Core.Simulation;
using DiamondX.Core.State;

[assembly: InternalsVisibleTo("DiamondX.Tests")]

namespace DiamondX.Core;

public class Game
{
    private readonly List<Player> _homeTeam;
    private readonly List<Player> _awayTeam;
    private readonly GameState _state = new();
    private readonly IPlateAppearanceResolver _plateAppearanceResolver;

    private int _homeBatterIndex;
    private int _awayBatterIndex;

    internal GameState State => _state;

    public Game(List<Player> homeTeam, List<Player> awayTeam, IPlateAppearanceResolver? plateAppearanceResolver = null)
    {
        _homeTeam = homeTeam ?? throw new ArgumentNullException(nameof(homeTeam));
        _awayTeam = awayTeam ?? throw new ArgumentNullException(nameof(awayTeam));

        if (_homeTeam.Count == 0)
        {
            throw new ArgumentException("Home team must have at least one player.", nameof(homeTeam));
        }

        if (_awayTeam.Count == 0)
        {
            throw new ArgumentException("Away team must have at least one player.", nameof(awayTeam));
        }

        _plateAppearanceResolver = plateAppearanceResolver ?? new PlateAppearanceResolver(new SystemRandomSource());

        Console.WriteLine("--- Game Start ---");
    }

    private AtBatOutcome SimulateAtBat(Player player)
    {
        Console.WriteLine($"At bat: {player.Name}");
        return _plateAppearanceResolver.Resolve(player);
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
            Console.WriteLine($"{batter.Name} hits a home run! A run scores!");
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
            Console.WriteLine($"{third.Name} scores!");
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
                Console.WriteLine($"{runner.Name} scores!");
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

        while (_state.Outs < 3)
        {
            PrintBases();
            Player currentBatter = battingTeam[batterIndex];
            AtBatOutcome outcome = SimulateAtBat(currentBatter);

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

        Console.WriteLine(new string('-', 20));
    }

    private void PrintBases()
    {
        Console.WriteLine($"Bases: [1B: {_state.Bases[0]?.Name ?? "___"}] [2B: {_state.Bases[1]?.Name ?? "___"}] [3B: {_state.Bases[2]?.Name ?? "___"}]");
    }

    public void PlayGame()
    {
        for (int inning = 1; inning <= 9; inning++)
        {
            _state.BeginHalfInning(inning, HalfInning.Top);
            Console.WriteLine($"\nTop of Inning {inning} | Score: Away {_state.AwayScore} - Home {_state.HomeScore}");
            PlayHalfInning(_awayTeam, isHomeTeam: false);

            if (inning == 9 && _state.HomeScore > _state.AwayScore)
            {
                break;
            }

            _state.BeginHalfInning(inning, HalfInning.Bottom);
            Console.WriteLine($"Bottom of Inning {inning} | Score: Away {_state.AwayScore} - Home {_state.HomeScore}");
            PlayHalfInning(_homeTeam, isHomeTeam: true);
        }

        PrintFinalScore();
    }

    private void PrintFinalScore()
    {
        Console.WriteLine("\n--- Game Over ---");
        Console.WriteLine($"Final Score: Away {_state.AwayScore} - Home {_state.HomeScore}");
        if (_state.HomeScore > _state.AwayScore)
        {
            Console.WriteLine("Home Team Wins!");
        }
        else if (_state.AwayScore > _state.HomeScore)
        {
            Console.WriteLine("Away Team Wins!");
        }
        else
        {
            Console.WriteLine("It's a tie!");
        }
    }
}