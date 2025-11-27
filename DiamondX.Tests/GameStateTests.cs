using DiamondX.Core.State;
using DiamondX.Core.Models;

namespace DiamondX.Tests;

public class GameStateTests
{
    [Test]
    public void BeginHalfInning_ResetsOutsAndClearsBases()
    {
        var state = new GameState();
        state.SetBase(0, new Player("A", 0, 0, 0, 0, 0));
        state.SetBase(1, new Player("B", 0, 0, 0, 0, 0));
        state.SetBase(2, new Player("C", 0, 0, 0, 0, 0));
        state.RecordOut();
        state.RecordOut();

        state.BeginHalfInning(3, HalfInning.Bottom);

        Assert.That(state.Outs, Is.EqualTo(0));
        Assert.That(state.Bases[0], Is.Null);
        Assert.That(state.Bases[1], Is.Null);
        Assert.That(state.Bases[2], Is.Null);
        Assert.That(state.Inning, Is.EqualTo(3));
        Assert.That(state.Half, Is.EqualTo(HalfInning.Bottom));
    }

    [Test]
    public void AddRun_UpdatesCorrectTeamScore()
    {
        var state = new GameState();
        state.AddRun(isHomeTeam: true);
        state.AddRun(isHomeTeam: false);
        state.AddRun(isHomeTeam: true);

        Assert.That(state.HomeScore, Is.EqualTo(2));
        Assert.That(state.AwayScore, Is.EqualTo(1));
    }
}
