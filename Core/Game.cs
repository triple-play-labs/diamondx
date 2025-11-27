// Game.cs
using System.Runtime.CompilerServices;
using DiamondX.Core.Events;
using DiamondX.Core.Events.Baseball;
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
    private readonly EventScheduler _eventScheduler;

    private int _homeBatterIndex;
    private int _awayBatterIndex;

    private Pitcher? _homePitcher;
    private Pitcher? _awayPitcher;

    internal GameState State => _state;

    /// <summary>
    /// Access to the event scheduler for registering handlers or querying events.
    /// </summary>
    public EventScheduler Events => _eventScheduler;

    public Game(List<Player> homeTeam, List<Player> awayTeam, IPlateAppearanceResolver? plateAppearanceResolver = null)
        : this(homeTeam, awayTeam, null, null, plateAppearanceResolver, null)
    {
    }

    public Game(
        List<Player> homeTeam,
        List<Player> awayTeam,
        Pitcher? homePitcher,
        Pitcher? awayPitcher,
        IPlateAppearanceResolver? plateAppearanceResolver = null)
        : this(homeTeam, awayTeam, homePitcher, awayPitcher, plateAppearanceResolver, null)
    {
    }

    public Game(
        List<Player> homeTeam,
        List<Player> awayTeam,
        Pitcher? homePitcher,
        Pitcher? awayPitcher,
        IPlateAppearanceResolver? plateAppearanceResolver,
        EventScheduler? eventScheduler)
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

        _homePitcher = homePitcher;
        _awayPitcher = awayPitcher;
        _plateAppearanceResolver = plateAppearanceResolver ?? new PlateAppearanceResolver(new SystemRandomSource());
        _eventScheduler = eventScheduler ?? new EventScheduler();
    }

    /// <summary>
    /// Set or replace the current pitcher for a team mid-game.
    /// </summary>
    public void SetPitcher(Pitcher pitcher, bool isHomeTeam)
    {
        var previous = isHomeTeam ? _homePitcher : _awayPitcher;

        if (isHomeTeam)
        {
            _homePitcher = pitcher;
        }
        else
        {
            _awayPitcher = pitcher;
        }

        _eventScheduler.Publish(new PitcherChangedEvent
        {
            NewPitcher = pitcher,
            PreviousPitcher = previous,
            IsHomeTeam = isHomeTeam,
            Inning = _state.Inning,
            Half = _state.Half
        });
    }

    private AtBatOutcome SimulateAtBat(Player batter, bool isHomeTeamBatting)
    {
        // When home team bats, away pitcher is on the mound (and vice versa)
        Pitcher? pitcher = isHomeTeamBatting ? _awayPitcher : _homePitcher;

        _eventScheduler.Publish(new AtBatStartedEvent
        {
            Batter = batter,
            Pitcher = pitcher,
            Inning = _state.Inning,
            Half = _state.Half,
            Outs = _state.Outs
        });

        AtBatOutcome outcome;
        if (pitcher != null)
        {
            outcome = _plateAppearanceResolver.Resolve(batter, pitcher);
        }
        else
        {
            outcome = _plateAppearanceResolver.Resolve(batter);
        }

        _eventScheduler.Publish(new AtBatCompletedEvent
        {
            Batter = batter,
            Pitcher = pitcher,
            Outcome = outcome,
            Inning = _state.Inning,
            Half = _state.Half
        });

        return outcome;
    }

    internal void AdvanceRunners(AtBatOutcome outcome, Player batter, bool isHomeTeam)
    {
        if (outcome == AtBatOutcome.Walk)
        {
            HandleWalk(batter, isHomeTeam, outcome);
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

        MoveExistingRunners(basesToAdvance, isHomeTeam, outcome, batter);

        if (basesToAdvance < 4)
        {
            _state.SetBase(basesToAdvance - 1, batter);
            _eventScheduler.Publish(new RunnerAdvancedEvent
            {
                Runner = batter,
                FromBase = 0,
                ToBase = basesToAdvance,
                Cause = outcome
            });
        }
        else
        {
            // Home run - batter scores
            _state.AddRun(isHomeTeam);
            _eventScheduler.Publish(new RunScoredEvent
            {
                Runner = batter,
                Batter = batter,
                IsHomeTeam = isHomeTeam,
                NewScore = isHomeTeam ? _state.HomeScore : _state.AwayScore,
                ScoringPlay = outcome
            });
        }
    }

    private void HandleWalk(Player batter, bool isHomeTeam, AtBatOutcome outcome)
    {
        var first = _state.GetBase(0);
        var second = _state.GetBase(1);
        var third = _state.GetBase(2);

        if (first != null && second != null && third != null)
        {
            _state.AddRun(isHomeTeam);
            _eventScheduler.Publish(new RunScoredEvent
            {
                Runner = third,
                Batter = batter,
                IsHomeTeam = isHomeTeam,
                NewScore = isHomeTeam ? _state.HomeScore : _state.AwayScore,
                ScoringPlay = outcome
            });
            third = second;
            second = first;
            first = batter;
        }
        else if (first != null && second != null)
        {
            _eventScheduler.Publish(new RunnerAdvancedEvent { Runner = second, FromBase = 2, ToBase = 3, Cause = outcome });
            third = second;
            _eventScheduler.Publish(new RunnerAdvancedEvent { Runner = first, FromBase = 1, ToBase = 2, Cause = outcome });
            second = first;
            first = batter;
        }
        else if (first != null)
        {
            _eventScheduler.Publish(new RunnerAdvancedEvent { Runner = first, FromBase = 1, ToBase = 2, Cause = outcome });
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

    private void MoveExistingRunners(int basesToAdvance, bool isHomeTeam, AtBatOutcome outcome, Player batter)
    {
        // Runners on base typically advance more aggressively than the batter
        // On a single: runners advance 2 bases (runner on 2nd scores)
        // On doubles/triples/HR: all runners score
        int runnerAdvance = basesToAdvance == 1 ? 2 : basesToAdvance;

        for (int baseIndex = 2; baseIndex >= 0; baseIndex--)
        {
            var runner = _state.GetBase(baseIndex);
            if (runner == null)
            {
                continue;
            }

            _state.SetBase(baseIndex, null);

            int destination = baseIndex + runnerAdvance;
            if (destination >= 3)
            {
                _state.AddRun(isHomeTeam);
                _eventScheduler.Publish(new RunScoredEvent
                {
                    Runner = runner,
                    Batter = batter,
                    IsHomeTeam = isHomeTeam,
                    NewScore = isHomeTeam ? _state.HomeScore : _state.AwayScore,
                    ScoringPlay = outcome
                });
            }
            else
            {
                _state.SetBase(destination, runner);
                _eventScheduler.Publish(new RunnerAdvancedEvent
                {
                    Runner = runner,
                    FromBase = baseIndex + 1,
                    ToBase = destination + 1,
                    Cause = outcome
                });
            }
        }
    }

    private void PlayHalfInning(List<Player> battingTeam, bool isHomeTeam)
    {
        int batterIndex = isHomeTeam ? _homeBatterIndex : _awayBatterIndex;
        int runsThisInning = isHomeTeam ? _state.HomeScore : _state.AwayScore;

        _eventScheduler.Publish(new InningStartedEvent
        {
            Inning = _state.Inning,
            Half = _state.Half,
            HomeScore = _state.HomeScore,
            AwayScore = _state.AwayScore
        });

        while (_state.Outs < 3)
        {
            Player currentBatter = battingTeam[batterIndex];
            AtBatOutcome outcome = SimulateAtBat(currentBatter, isHomeTeam);

            switch (outcome)
            {
                case AtBatOutcome.Out:
                    _state.RecordOut();
                    _eventScheduler.Publish(new OutRecordedEvent
                    {
                        Batter = currentBatter,
                        OutNumber = _state.Outs,
                        Inning = _state.Inning,
                        Half = _state.Half
                    });
                    break;
                default:
                    AdvanceRunners(outcome, currentBatter, isHomeTeam);
                    break;
            }

            batterIndex = (batterIndex + 1) % battingTeam.Count;
        }

        int runsScored = (isHomeTeam ? _state.HomeScore : _state.AwayScore) - runsThisInning;
        _eventScheduler.Publish(new InningEndedEvent
        {
            Inning = _state.Inning,
            Half = _state.Half,
            RunsScored = runsScored
        });

        if (isHomeTeam)
        {
            _homeBatterIndex = batterIndex;
        }
        else
        {
            _awayBatterIndex = batterIndex;
        }
    }

    public void PlayGame()
    {
        _eventScheduler.Publish(new GameStartedEvent
        {
            HomeTeamName = "Home",
            AwayTeamName = "Away",
            HomePitcher = _homePitcher,
            AwayPitcher = _awayPitcher
        });

        for (int inning = 1; inning <= 9; inning++)
        {
            _state.BeginHalfInning(inning, HalfInning.Top);
            PlayHalfInning(_awayTeam, isHomeTeam: false);

            if (inning == 9 && _state.HomeScore > _state.AwayScore)
            {
                break;
            }

            _state.BeginHalfInning(inning, HalfInning.Bottom);
            PlayHalfInning(_homeTeam, isHomeTeam: true);
        }

        _eventScheduler.Publish(new GameEndedEvent
        {
            HomeScore = _state.HomeScore,
            AwayScore = _state.AwayScore,
            Innings = _state.Inning
        });
    }
}
