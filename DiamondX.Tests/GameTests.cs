using DiamondX.Core;
using DiamondX.Core.Models;

namespace DiamondX.Tests;

public class GameTests
{
    private Player _batter;
    private Game _game;

    [SetUp]
    public void Setup()
    {
        _batter = new Player("Test Batter", 0.1, 0.2, 0.05, 0.01, 0.05);
        var team1 = new List<Player> { _batter };
        var team2 = new List<Player> { new Player("Pitcher", 0,0,0,0,0) };
        _game = new Game(team1, team2);
    }

    [Test]
    public void AdvanceRunners_Single_EmptyBases_BatterOnFirst()
    {
        // Arrange
        var outcome = AtBatOutcome.Single;

        // Act
        _game.AdvanceRunners(outcome, _batter, true);

        // Assert
        Assert.That(_game._bases[0], Is.EqualTo(_batter));
        Assert.That(_game._bases[1], Is.Null);
        Assert.That(_game._bases[2], Is.Null);
    }
}
