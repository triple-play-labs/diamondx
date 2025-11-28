using DiamondX.Core.Models;
using DiamondX.Core.State;
using SimulationEngine.Events;

namespace DiamondX.Core.Events.Baseball;

/// <summary>
/// Event type constants for baseball events.
/// </summary>
public static class BaseballEventTypes
{
    public const string GameStarted = "baseball.game.started";
    public const string GameEnded = "baseball.game.ended";
    public const string InningStarted = "baseball.inning.started";
    public const string InningEnded = "baseball.inning.ended";
    public const string AtBatStarted = "baseball.atbat.started";
    public const string AtBatCompleted = "baseball.atbat.completed";
    public const string RunScored = "baseball.run.scored";
    public const string OutRecorded = "baseball.out.recorded";
    public const string RunnerAdvanced = "baseball.runner.advanced";
    public const string PitcherChanged = "baseball.pitcher.changed";
}

/// <summary>
/// Emitted when a game starts.
/// </summary>
public record GameStartedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.GameStarted;
    public required string HomeTeamName { get; init; }
    public required string AwayTeamName { get; init; }
    public Pitcher? HomePitcher { get; init; }
    public Pitcher? AwayPitcher { get; init; }
}

/// <summary>
/// Emitted when a game ends.
/// </summary>
public record GameEndedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.GameEnded;
    public required int HomeScore { get; init; }
    public required int AwayScore { get; init; }
    public required int Innings { get; init; }
}

/// <summary>
/// Emitted at the start of each half-inning.
/// </summary>
public record InningStartedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.InningStarted;
    public required int Inning { get; init; }
    public required HalfInning Half { get; init; }
    public required int HomeScore { get; init; }
    public required int AwayScore { get; init; }
}

/// <summary>
/// Emitted at the end of each half-inning.
/// </summary>
public record InningEndedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.InningEnded;
    public required int Inning { get; init; }
    public required HalfInning Half { get; init; }
    public required int RunsScored { get; init; }
}

/// <summary>
/// Emitted when a batter steps to the plate.
/// </summary>
public record AtBatStartedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.AtBatStarted;
    public required Player Batter { get; init; }
    public required Pitcher? Pitcher { get; init; }
    public required int Inning { get; init; }
    public required HalfInning Half { get; init; }
    public required int Outs { get; init; }
}

/// <summary>
/// Emitted when a plate appearance completes.
/// </summary>
public record AtBatCompletedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.AtBatCompleted;
    public required Player Batter { get; init; }
    public required Pitcher? Pitcher { get; init; }
    public required AtBatOutcome Outcome { get; init; }
    public required int Inning { get; init; }
    public required HalfInning Half { get; init; }
}

/// <summary>
/// Emitted when a run scores.
/// </summary>
public record RunScoredEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.RunScored;
    public required Player Runner { get; init; }
    public required Player Batter { get; init; }
    public required bool IsHomeTeam { get; init; }
    public required int NewScore { get; init; }
    public required AtBatOutcome ScoringPlay { get; init; }
}

/// <summary>
/// Emitted when an out is recorded.
/// </summary>
public record OutRecordedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.OutRecorded;
    public required Player Batter { get; init; }
    public required int OutNumber { get; init; }
    public required int Inning { get; init; }
    public required HalfInning Half { get; init; }
}

/// <summary>
/// Emitted when a runner advances bases.
/// </summary>
public record RunnerAdvancedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.RunnerAdvanced;
    public required Player Runner { get; init; }
    public required int FromBase { get; init; }
    public required int ToBase { get; init; }
    public required AtBatOutcome Cause { get; init; }
}

/// <summary>
/// Emitted when a pitcher is changed.
/// </summary>
public record PitcherChangedEvent : SimulationEventBase
{
    public override string EventType => BaseballEventTypes.PitcherChanged;
    public required Pitcher NewPitcher { get; init; }
    public Pitcher? PreviousPitcher { get; init; }
    public required bool IsHomeTeam { get; init; }
    public required int Inning { get; init; }
    public required HalfInning Half { get; init; }
}
