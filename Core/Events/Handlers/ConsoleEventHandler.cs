using DiamondX.Core.Events.Baseball;
using DiamondX.Core.Models;
using DiamondX.Core.State;

namespace DiamondX.Core.Events.Handlers;

/// <summary>
/// Event handler that prints game events to the console in a human-readable format.
/// </summary>
public class ConsoleEventHandler : IEventHandler
{
    public IEnumerable<string>? EventTypeFilter => null; // Receive all events

    public void Handle(ISimulationEvent simulationEvent)
    {
        var message = simulationEvent switch
        {
            GameStartedEvent e => $"\n{'='.Repeat(50)}\n⚾ GAME START: {e.AwayTeamName} @ {e.HomeTeamName}\n{'='.Repeat(50)}",

            GameEndedEvent e => $"\n{'='.Repeat(50)}\n⚾ FINAL SCORE: Away {e.AwayScore} - Home {e.HomeScore} ({e.Innings} innings)\n{'='.Repeat(50)}\n",

            InningStartedEvent e => $"\n--- {FormatHalf(e.Half)} of Inning {e.Inning} | Score: Away {e.AwayScore} - Home {e.HomeScore} ---",

            InningEndedEvent e => $"    [{e.RunsScored} run(s) scored this half-inning]",

            AtBatStartedEvent e => $"  At bat: {e.Batter.Name}" + (e.Pitcher != null ? $" vs {e.Pitcher.Name}" : ""),

            AtBatCompletedEvent e => $"    Result: {FormatOutcome(e.Outcome)}",

            RunScoredEvent e => $"    🏃 {e.Runner.Name} SCORES! ({(e.IsHomeTeam ? "Home" : "Away")} now has {e.NewScore})",

            OutRecordedEvent e => $"    Out #{e.OutNumber}",

            RunnerAdvancedEvent e => $"    → {e.Runner.Name} advances to {FormatBase(e.ToBase)}",

            PitcherChangedEvent e => $"  ⚾ Pitching change: {e.NewPitcher.Name} enters" + (e.PreviousPitcher != null ? $" (replaces {e.PreviousPitcher.Name})" : ""),

            _ => null
        };

        if (message != null)
        {
            Console.WriteLine(message);
        }
    }

    private static string FormatHalf(HalfInning half) => half == HalfInning.Top ? "Top" : "Bottom";

    private static string FormatOutcome(AtBatOutcome outcome) => outcome switch
    {
        AtBatOutcome.Out => "OUT",
        AtBatOutcome.Walk => "WALK",
        AtBatOutcome.Single => "SINGLE!",
        AtBatOutcome.Double => "DOUBLE!",
        AtBatOutcome.Triple => "TRIPLE!",
        AtBatOutcome.HomeRun => "HOME RUN! 💥",
        _ => outcome.ToString()
    };

    private static string FormatBase(int baseNumber) => baseNumber switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"base {baseNumber}"
    };
}

internal static class StringExtensions
{
    public static string Repeat(this char c, int count) => new(c, count);
}
