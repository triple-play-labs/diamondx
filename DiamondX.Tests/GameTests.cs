using DiamondX.Core;
using DiamondX.Core.Models;
using DiamondX.Core.State;

namespace DiamondX.Tests;

public class GameTests
{
    private Player _batter = null!;
    private Game _game = null!;

    [SetUp]
    public void Setup()
    {
        _batter = new Player("Test Batter", 0.1, 0.2, 0.05, 0.01, 0.05);
        var team1 = new List<Player> { _batter };
        var team2 = new List<Player> { new Player("Pitcher", 0, 0, 0, 0, 0) };
        _game = new Game(team1, team2);
        // Initialize inning state for testing
        _game.State.BeginHalfInning(1, HalfInning.Top);
    }

    [Test]
    public void AdvanceRunners_Single_EmptyBases_BatterOnFirst()
    {
        // Arrange
        var outcome = AtBatOutcome.Single;

        // Act
        _game.AdvanceRunners(outcome, _batter, true);

        // Assert
        Assert.That(_game.State.GetBase(0), Is.EqualTo(_batter));
        Assert.That(_game.State.GetBase(1), Is.Null);
        Assert.That(_game.State.GetBase(2), Is.Null);
    }

    [Test]
    public void AdvanceRunners_Double_EmptyBases_BatterOnSecond()
    {
        // Arrange
        var outcome = AtBatOutcome.Double;

        // Act
        _game.AdvanceRunners(outcome, _batter, true);

        // Assert
        Assert.That(_game.State.GetBase(0), Is.Null);
        Assert.That(_game.State.GetBase(1), Is.EqualTo(_batter));
        Assert.That(_game.State.GetBase(2), Is.Null);
    }

    [Test]
    public void AdvanceRunners_Triple_EmptyBases_BatterOnThird()
    {
        // Arrange
        var outcome = AtBatOutcome.Triple;

        // Act
        _game.AdvanceRunners(outcome, _batter, true);

        // Assert
        Assert.That(_game.State.GetBase(0), Is.Null);
        Assert.That(_game.State.GetBase(1), Is.Null);
        Assert.That(_game.State.GetBase(2), Is.EqualTo(_batter));
    }

    [Test]
    public void AdvanceRunners_Walk_EmptyBases_BatterOnFirst()
    {
        // Arrange
        var outcome = AtBatOutcome.Walk;

        // Act
        _game.AdvanceRunners(outcome, _batter, true);

        // Assert
        Assert.That(_game.State.GetBase(0), Is.EqualTo(_batter));
        Assert.That(_game.State.GetBase(1), Is.Null);
        Assert.That(_game.State.GetBase(2), Is.Null);
    }

    [Test]
    public void AdvanceRunners_Walk_RunnerOnFirst_ForcedToSecond()
    {
        // Arrange
        var existingRunner = new Player("Runner", 0.1, 0.2, 0.05, 0.01, 0.05);
        _game.State.SetBase(0, existingRunner);
        var outcome = AtBatOutcome.Walk;

        // Act
        _game.AdvanceRunners(outcome, _batter, true);

        // Assert
        Assert.That(_game.State.GetBase(0), Is.EqualTo(_batter));
        Assert.That(_game.State.GetBase(1), Is.EqualTo(existingRunner));
        Assert.That(_game.State.GetBase(2), Is.Null);
    }

    [Test]
    public void AdvanceRunners_Single_RunnerOnSecond_RunnerScores()
    {
        // Arrange
        var runner = new Player("Runner", 0.1, 0.2, 0.05, 0.01, 0.05);
        _game.State.SetBase(1, runner); // Runner on second
        var initialScore = _game.State.HomeScore;

        // Act
        _game.AdvanceRunners(AtBatOutcome.Single, _batter, true);

        // Assert
        Assert.That(_game.State.HomeScore, Is.EqualTo(initialScore + 1));
        Assert.That(_game.State.GetBase(0), Is.EqualTo(_batter));
    }
}
