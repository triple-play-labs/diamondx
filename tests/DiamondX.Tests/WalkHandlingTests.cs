using DiamondX.Core;
using DiamondX.Core.Models;

namespace DiamondX.Tests;

public class WalkHandlingTests
{
    private static Player B(string name) => new Player(name, 0, 0, 0, 0, 0);

    [Test]
    public void Walk_ForcesAdvance_WhenFirstOccupied()
    {
        var batter = new Player("Batter", 0, 0, 0, 0, 0);
        var home = new List<Player> { batter };
        var away = new List<Player> { B("P") };
        var game = new Game(home, away);

        // Put runner on first
        game.State.SetBase(0, B("R1"));

        game.AdvanceRunners(AtBatOutcome.Walk, batter, isHomeTeam: true);

        Assert.That(game.State.Bases[0]?.Name, Is.EqualTo("Batter"));
        Assert.That(game.State.Bases[1]?.Name, Is.EqualTo("R1"));
        Assert.That(game.State.Bases[2], Is.Null);
        Assert.That(game.State.HomeScore, Is.EqualTo(0));
    }

    [Test]
    public void Walk_BasesLoaded_ScoresOneRun()
    {
        var batter = new Player("Batter", 0, 0, 0, 0, 0);
        var home = new List<Player> { batter };
        var away = new List<Player> { B("P") };
        var game = new Game(home, away);

        game.State.SetBase(0, B("R1"));
        game.State.SetBase(1, B("R2"));
        game.State.SetBase(2, B("R3"));

        game.AdvanceRunners(AtBatOutcome.Walk, batter, isHomeTeam: true);

        Assert.That(game.State.HomeScore, Is.EqualTo(1));
        Assert.That(game.State.Bases[0]?.Name, Is.EqualTo("Batter"));
        Assert.That(game.State.Bases[1]?.Name, Is.EqualTo("R1"));
        Assert.That(game.State.Bases[2]?.Name, Is.EqualTo("R2"));
    }
}
