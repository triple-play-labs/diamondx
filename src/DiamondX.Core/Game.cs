// Game.cs
using System;
using System.Runtime.CompilerServices;
using DiamondX.Core.Models;
using DiamondX.Core.Simulation;
using DiamondX.Core.State;
using SimulationEngine.Random;

[assembly: InternalsVisibleTo("DiamondX.Tests")]

namespace DiamondX.Core;

/// <summary>
/// Configuration for creating a baseball game.
/// </summary>
public class GameOptions
{
    public required List<Player> HomeTeam { get; init; }
    public required List<Player> AwayTeam { get; init; }
    public string HomeTeamName { get; init; } = "Home";
    public string AwayTeamName { get; init; } = "Away";
    public Pitcher? HomePitcher { get; init; }
    public Pitcher? AwayPitcher { get; init; }
    public IPlateAppearanceResolver? PlateAppearanceResolver { get; init; }
    public bool Verbose { get; init; } = true;
}

public class Game
{
    private readonly List<Player> _homeTeam;
    private readonly List<Player> _awayTeam;
    private readonly string _homeTeamName;
    private readonly string _awayTeamName;
    private readonly GameState _state = new();
    private readonly IPlateAppearanceResolver _plateAppearanceResolver;
    private readonly bool _verbose;

    private readonly Pitcher _homePitcher;
    private readonly Pitcher _awayPitcher;

    private int _homeBatterIndex;
    private int _awayBatterIndex;
    private int _totalPlateAppearances;
    private bool _gameStarted;
    private bool _gameOver;

    internal GameState State => _state;
    public string HomeTeamName => _homeTeamName;
    public string AwayTeamName => _awayTeamName;
    public Pitcher HomePitcher => _homePitcher;
    public Pitcher AwayPitcher => _awayPitcher;

    /// <summary>
    /// Returns true if the game has ended.
    /// </summary>
    public bool IsGameOver => _gameOver;

    /// <summary>
    /// Total plate appearances in the game so far.
    /// </summary>
    public int TotalPlateAppearances => _totalPlateAppearances;

    /// <summary>
    /// Current score for home team.
    /// </summary>
    public int HomeScore => _state.HomeScore;

    /// <summary>
    /// Current score for away team.
    /// </summary>
    public int AwayScore => _state.AwayScore;

    /// <summary>
    /// Current inning number.
    /// </summary>
    public int CurrentInning => _state.Inning;

    /// <summary>
    /// Creates a game with the specified options.
    /// </summary>
    public Game(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _homeTeam = options.HomeTeam ?? throw new ArgumentNullException(nameof(options.HomeTeam));
        _awayTeam = options.AwayTeam ?? throw new ArgumentNullException(nameof(options.AwayTeam));
        _homeTeamName = options.HomeTeamName;
        _awayTeamName = options.AwayTeamName;
        _verbose = options.Verbose;

        if (_homeTeam.Count == 0)
            throw new ArgumentException("Home team must have at least one player.", nameof(options.HomeTeam));

        if (_awayTeam.Count == 0)
            throw new ArgumentException("Away team must have at least one player.", nameof(options.AwayTeam));

        _homePitcher = options.HomePitcher ?? CreateDefaultPitcher($"{_homeTeamName} Starter");
        _awayPitcher = options.AwayPitcher ?? CreateDefaultPitcher($"{_awayTeamName} Starter");
        _plateAppearanceResolver = options.PlateAppearanceResolver ?? new PlateAppearanceResolver(new SystemRandomSource());

        Log($"--- Game Start: {_awayTeamName} @ {_homeTeamName} ---");
    }

    /// <summary>
    /// Creates a game with individual parameters (legacy constructor).
    /// </summary>
    public Game(
        List<Player> homeTeam,
        List<Player> awayTeam,
        string homeTeamName = "Home",
        string awayTeamName = "Away",
        Pitcher? homePitcher = null,
        Pitcher? awayPitcher = null,
        IPlateAppearanceResolver? plateAppearanceResolver = null,
        bool verbose = true)
        : this(new GameOptions
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            HomeTeamName = homeTeamName,
            AwayTeamName = awayTeamName,
            HomePitcher = homePitcher,
            AwayPitcher = awayPitcher,
            PlateAppearanceResolver = plateAppearanceResolver,
            Verbose = verbose
        })
    {
    }

    private void Log(string message)
    {
        if (_verbose)
            Console.WriteLine(message);
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

    /// <summary>
    /// Executes a single plate appearance. This is the step-by-step execution method
    /// for use with the simulation engine.
    /// </summary>
    /// <returns>True if the game is still in progress, false if game is over.</returns>
    public bool PlayPlateAppearance()
    {
        if (_gameOver)
            return false;

        // Start game if not started
        if (!_gameStarted)
        {
            _gameStarted = true;
            _state.BeginHalfInning(1, HalfInning.Top);
            Log($"\nTop of Inning 1 | Score: {_awayTeamName} 0 - {_homeTeamName} 0");
        }

        // Determine current batting team and pitcher
        bool isHomeTeam = _state.Half == HalfInning.Bottom;
        var battingTeam = isHomeTeam ? _homeTeam : _awayTeam;
        var pitcher = isHomeTeam ? _awayPitcher : _homePitcher;
        int batterIndex = isHomeTeam ? _homeBatterIndex : _awayBatterIndex;

        // Execute the plate appearance
        Log($"Bases: [1B: {_state.Bases[0]?.Name ?? "___"}] [2B: {_state.Bases[1]?.Name ?? "___"}] [3B: {_state.Bases[2]?.Name ?? "___"}]");

        Player currentBatter = battingTeam[batterIndex];
        Log($"At bat: {currentBatter.Name} vs {pitcher.Name} (Pitch #{pitcher.PitchCount + 1}, Fatigue: {pitcher.FatigueLevel:P0})");

        AtBatOutcome outcome = _plateAppearanceResolver.Resolve(currentBatter, pitcher);
        _totalPlateAppearances++;

        // Process the outcome
        ProcessOutcome(outcome, currentBatter, isHomeTeam);

        // Advance batter index
        batterIndex = (batterIndex + 1) % battingTeam.Count;
        if (isHomeTeam)
            _homeBatterIndex = batterIndex;
        else
            _awayBatterIndex = batterIndex;

        // Check if half-inning is over (3 outs)
        if (_state.Outs >= 3)
        {
            EndHalfInning(pitcher);
        }

        return !_gameOver;
    }

    private void ProcessOutcome(AtBatOutcome outcome, Player batter, bool isHomeTeam)
    {
        switch (outcome)
        {
            case AtBatOutcome.Out:
                Log("Result: OUT!");
                _state.RecordOut();
                break;
            case AtBatOutcome.Walk:
                Log("Result: WALK!");
                AdvanceRunners(outcome, batter, isHomeTeam);
                break;
            default:
                Log($"Result: {outcome.ToString().ToUpper()}!");
                AdvanceRunners(outcome, batter, isHomeTeam);
                break;
        }

        // Check for walk-off in bottom of 9th or later
        if (_state.Half == HalfInning.Bottom && _state.Inning >= 9 && _state.HomeScore > _state.AwayScore)
        {
            EndGame();
        }
    }

    private void EndHalfInning(Pitcher pitcher)
    {
        Log($"[{pitcher.Name}: {pitcher.PitchCount} pitches, {pitcher.FatigueLevel:P0} fatigue]");
        Log(new string('-', 20));

        if (_state.Half == HalfInning.Top)
        {
            // Check if home team already leads after top of 9th or later (no need to bat)
            if (_state.Inning >= 9 && _state.HomeScore > _state.AwayScore)
            {
                EndGame();
                return;
            }

            // Start bottom of inning
            _state.BeginHalfInning(_state.Inning, HalfInning.Bottom);
            Log($"Bottom of Inning {_state.Inning} | Score: {_awayTeamName} {_state.AwayScore} - {_homeTeamName} {_state.HomeScore}");
        }
        else
        {
            // End of full inning
            int nextInning = _state.Inning + 1;

            // Check if game is over (9+ innings and not tied)
            if (_state.Inning >= 9 && _state.HomeScore != _state.AwayScore)
            {
                EndGame();
                return;
            }

            // Start next inning
            if (_state.Inning >= 9 && _state.HomeScore == _state.AwayScore)
            {
                Log($"\n⚾ EXTRA INNINGS! ⚾");
            }

            _state.BeginHalfInning(nextInning, HalfInning.Top);
            Log($"\nTop of Inning {nextInning} | Score: {_awayTeamName} {_state.AwayScore} - {_homeTeamName} {_state.HomeScore}");
        }
    }

    private void EndGame()
    {
        _gameOver = true;
        Log("\n--- Game Over ---");
        string inningNote = _state.Inning > 9 ? $" ({_state.Inning} innings)" : "";
        Log($"Final Score: {_awayTeamName} {_state.AwayScore} - {_homeTeamName} {_state.HomeScore}{inningNote}");
        if (_state.HomeScore > _state.AwayScore)
            Log($"{_homeTeamName} Win!");
        else
            Log($"{_awayTeamName} Win!");
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
            return;

        MoveExistingRunners(basesToAdvance, isHomeTeam);

        if (basesToAdvance < 4)
        {
            _state.SetBase(basesToAdvance - 1, batter);
        }
        else
        {
            _state.AddRun(isHomeTeam);
            Log($"{batter.Name} hits a home run! ({(isHomeTeam ? _homeTeamName : _awayTeamName)} now has {(isHomeTeam ? _state.HomeScore : _state.AwayScore)})");
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
            Log($"{third.Name} scores! ({(isHomeTeam ? _homeTeamName : _awayTeamName)} now has {(isHomeTeam ? _state.HomeScore : _state.AwayScore)})");
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
                continue;

            _state.SetBase(baseIndex, null);

            int destination = baseIndex + basesToAdvance;
            if (destination >= 3)
            {
                _state.AddRun(isHomeTeam);
                Log($"{runner.Name} scores! ({(isHomeTeam ? _homeTeamName : _awayTeamName)} now has {(isHomeTeam ? _state.HomeScore : _state.AwayScore)})");
            }
            else
            {
                _state.SetBase(destination, runner);
            }
        }
    }

    /// <summary>
    /// Plays the entire game to completion (legacy method).
    /// For step-by-step execution, use PlayPlateAppearance() instead.
    /// </summary>
    public void PlayGame()
    {
        while (PlayPlateAppearance())
        {
            // Continue until game is over
        }
    }
}
