using NUnit.Framework;
using DiamondX.Core.Events;
using DiamondX.Core.Events.Baseball;
using DiamondX.Core.Models;
using DiamondX.Core.State;

namespace DiamondX.Tests;

[TestFixture]
public class EventSchedulerTests
{
    private EventScheduler _scheduler = null!;
    private Player _testPlayer = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduler = new EventScheduler();
        _testPlayer = new Player("Test", 0.080, 0.170, 0.050, 0.005, 0.030);
    }

    [Test]
    public void Publish_AssignsSequenceNumbers()
    {
        var event1 = new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" };
        var event2 = new InningStartedEvent { Inning = 1, Half = HalfInning.Top, HomeScore = 0, AwayScore = 0 };
        var event3 = new GameEndedEvent { HomeScore = 5, AwayScore = 3, Innings = 9 };

        _scheduler.Publish(event1);
        _scheduler.Publish(event2);
        _scheduler.Publish(event3);

        var events = _scheduler.EventLog.ToList();
        Assert.That(events[0].Sequence, Is.EqualTo(1));
        Assert.That(events[1].Sequence, Is.EqualTo(2));
        Assert.That(events[2].Sequence, Is.EqualTo(3));
    }

    [Test]
    public void Publish_AssignsTimestampFromSimulationTime()
    {
        _scheduler.SetTime(TimeSpan.FromMinutes(30));
        var gameEvent = new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" };

        _scheduler.Publish(gameEvent);

        var events = _scheduler.EventLog.ToList();
        Assert.That(events[0].Timestamp, Is.EqualTo(TimeSpan.FromMinutes(30)));
    }

    [Test]
    public void GetEvents_FiltersOnEventType()
    {
        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });
        _scheduler.Publish(new InningStartedEvent { Inning = 1, Half = HalfInning.Top, HomeScore = 0, AwayScore = 0 });
        _scheduler.Publish(new RunScoredEvent { Runner = _testPlayer, Batter = _testPlayer, IsHomeTeam = true, NewScore = 1, ScoringPlay = AtBatOutcome.HomeRun });
        _scheduler.Publish(new RunScoredEvent { Runner = _testPlayer, Batter = _testPlayer, IsHomeTeam = true, NewScore = 2, ScoringPlay = AtBatOutcome.HomeRun });
        _scheduler.Publish(new InningEndedEvent { Inning = 1, Half = HalfInning.Top, RunsScored = 2 });

        var runEvents = _scheduler.GetEvents<RunScoredEvent>().ToList();

        Assert.That(runEvents, Has.Count.EqualTo(2));
        Assert.That(runEvents[0].NewScore, Is.EqualTo(1));
        Assert.That(runEvents[1].NewScore, Is.EqualTo(2));
    }

    [Test]
    public void RegisterHandler_ReceivesMatchingEvents()
    {
        var receivedEvents = new List<ISimulationEvent>();
        var handler = new TestHandler(new[] { BaseballEventTypes.RunScored }, e => receivedEvents.Add(e));

        _scheduler.RegisterHandler(handler);
        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });
        _scheduler.Publish(new RunScoredEvent { Runner = _testPlayer, Batter = _testPlayer, IsHomeTeam = true, NewScore = 1, ScoringPlay = AtBatOutcome.HomeRun });
        _scheduler.Publish(new OutRecordedEvent { Batter = _testPlayer, OutNumber = 1, Inning = 1, Half = HalfInning.Top });
        _scheduler.Publish(new RunScoredEvent { Runner = _testPlayer, Batter = _testPlayer, IsHomeTeam = true, NewScore = 2, ScoringPlay = AtBatOutcome.HomeRun });

        Assert.That(receivedEvents, Has.Count.EqualTo(2));
        Assert.That(receivedEvents.All(e => e.EventType == BaseballEventTypes.RunScored), Is.True);
    }

    [Test]
    public void RegisterHandler_WithNullFilter_ReceivesAllEvents()
    {
        var receivedEvents = new List<ISimulationEvent>();
        var handler = new TestHandler(null, e => receivedEvents.Add(e));

        _scheduler.RegisterHandler(handler);
        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });
        _scheduler.Publish(new RunScoredEvent { Runner = _testPlayer, Batter = _testPlayer, IsHomeTeam = true, NewScore = 1, ScoringPlay = AtBatOutcome.HomeRun });
        _scheduler.Publish(new OutRecordedEvent { Batter = _testPlayer, OutNumber = 1, Inning = 1, Half = HalfInning.Top });

        Assert.That(receivedEvents, Has.Count.EqualTo(3));
    }

    [Test]
    public void ClearLog_RemovesAllEvents()
    {
        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });
        _scheduler.Publish(new InningStartedEvent { Inning = 1, Half = HalfInning.Top, HomeScore = 0, AwayScore = 0 });

        _scheduler.ClearLog();

        Assert.That(_scheduler.EventLog, Is.Empty);
    }

    [Test]
    public void Reset_ResetsSequenceCounter()
    {
        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });
        _scheduler.Publish(new InningStartedEvent { Inning = 1, Half = HalfInning.Top, HomeScore = 0, AwayScore = 0 });
        _scheduler.Reset();

        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });

        var events = _scheduler.EventLog.ToList();
        Assert.That(events[0].Sequence, Is.EqualTo(1));
    }

    [Test]
    public void MultipleHandlers_AllReceiveEvents()
    {
        var handler1Events = new List<ISimulationEvent>();
        var handler2Events = new List<ISimulationEvent>();

        _scheduler.RegisterHandler(new TestHandler(null, e => handler1Events.Add(e)));
        _scheduler.RegisterHandler(new TestHandler(null, e => handler2Events.Add(e)));

        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });

        Assert.That(handler1Events, Has.Count.EqualTo(1));
        Assert.That(handler2Events, Has.Count.EqualTo(1));
    }

    [Test]
    public void AdvanceTime_UpdatesTimestampForNextEvent()
    {
        _scheduler.Publish(new GameStartedEvent { HomeTeamName = "Home", AwayTeamName = "Away" });
        _scheduler.AdvanceTime(TimeSpan.FromMinutes(10));
        _scheduler.Publish(new InningStartedEvent { Inning = 1, Half = HalfInning.Top, HomeScore = 0, AwayScore = 0 });

        var events = _scheduler.EventLog.ToList();
        Assert.That(events[0].Timestamp, Is.EqualTo(TimeSpan.Zero));
        Assert.That(events[1].Timestamp, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }

    [Test]
    public void Game_EmitsExpectedEvents()
    {
        // Arrange a game that will have predictable outcomes
        var random = new TestRandomSource(new Queue<double>(Enumerable.Repeat(0.99, 200)));

        var homeTeam = CreateTestTeam("Home");
        var awayTeam = CreateTestTeam("Away");
        var game = new DiamondX.Core.Game(homeTeam, awayTeam, null, null,
            new DiamondX.Core.Simulation.PlateAppearanceResolver(random));

        // Act
        game.PlayGame();

        // Assert - verify key events were emitted
        var events = game.Events.EventLog.ToList();

        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.GameStarted), Is.True);
        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.InningStarted), Is.True);
        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.AtBatStarted), Is.True);
        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.AtBatCompleted), Is.True);
        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.OutRecorded), Is.True);
        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.InningEnded), Is.True);
        Assert.That(events.Any(e => e.EventType == BaseballEventTypes.GameEnded), Is.True);
    }

    [Test]
    public void Game_EventsAreInOrder()
    {
        var random = new TestRandomSource(new Queue<double>(Enumerable.Repeat(0.99, 200)));
        var homeTeam = CreateTestTeam("Home");
        var awayTeam = CreateTestTeam("Away");
        var game = new DiamondX.Core.Game(homeTeam, awayTeam, null, null,
            new DiamondX.Core.Simulation.PlateAppearanceResolver(random));

        game.PlayGame();

        var events = game.Events.EventLog.ToList();

        // Verify sequence numbers are strictly increasing
        for (int i = 1; i < events.Count; i++)
        {
            Assert.That(events[i].Sequence, Is.GreaterThan(events[i - 1].Sequence));
        }

        // Verify first event is GameStarted and last is GameEnded
        Assert.That(events[0].EventType, Is.EqualTo(BaseballEventTypes.GameStarted));
        Assert.That(events[^1].EventType, Is.EqualTo(BaseballEventTypes.GameEnded));
    }

    private static List<DiamondX.Core.Models.Player> CreateTestTeam(string prefix)
    {
        return Enumerable.Range(1, 9)
            .Select(i => new DiamondX.Core.Models.Player($"{prefix}{i}", 0.080, 0.170, 0.050, 0.005, 0.030))
            .ToList();
    }

    private sealed class TestHandler : IEventHandler
    {
        private readonly IEnumerable<string>? _eventTypeFilter;
        private readonly Action<ISimulationEvent> _action;

        public TestHandler(IEnumerable<string>? eventTypeFilter, Action<ISimulationEvent> action)
        {
            _eventTypeFilter = eventTypeFilter;
            _action = action;
        }

        public IEnumerable<string>? EventTypeFilter => _eventTypeFilter;

        public void Handle(ISimulationEvent simulationEvent)
        {
            _action(simulationEvent);
        }
    }
}
