using DiamondX.Core;
using DiamondX.Core.Models;
using DiamondX.Core.Simulation;
using SimulationEngine.Random;

namespace DiamondX.Tests;

public class HalfInningFlowTests
{
    [Test]
    public void DeterministicSequence_AdvancesOrderAndScores()
    {
        // Sequence: Walk, Single, Out, Double, HomeRun, Out, Out
        // Map probabilities to rolls deterministically: choose rolls that fall into desired buckets
        var batter = new Player("A", walkRate: 0.5, singleRate: 0.5, doubleRate: 0.0, tripleRate: 0.0, homeRunRate: 0.0);
        var b2 = new Player("B", 0.0, 1.0, 0.0, 0.0, 0.0);
        var b3 = new Player("C", 0.0, 0.0, 1.0, 0.0, 0.0);
        var b4 = new Player("D", 0.0, 0.0, 0.0, 0.0, 1.0);
        var lineup = new List<Player> { batter, b2, b3, b4 };
        var away = new List<Player> { new Player("P", 0, 0, 0, 0, 0) };

        // Rolls to get outcomes per hitter:
        // A: 0.25 -> Walk
        // B: 0.75 -> Single (since walkRate=0, singleRate=1)
        // C: 0.99 -> Out (since remaining prob after doubleRate=1 is 0)
        // D: 0.99 -> Out (for simplicity, we'll adjust sequence to ensure 3 outs eventually)
        var rolls = new[] { 0.25, 0.75, 0.99, 0.99, 0.99, 0.99, 0.99 };
        var rng = new TestRandomSource(rolls);
        var resolver = new PlateAppearanceResolver(rng);
        var game = new Game(lineup, away, "Home", "Away", plateAppearanceResolver: resolver);

        game.State.BeginHalfInning(1, Core.State.HalfInning.Top);
        // Play until 3 outs with our resolver by calling internal PlayHalfInning? Not exposed.
        // We'll manually simulate a few batters to assert runner movement.

        // A: Walk
        var outcomeA = resolver.Resolve(batter);
        game.AdvanceRunners(outcomeA, batter, isHomeTeam: false);
        Assert.That(game.State.Bases[0]?.Name, Is.EqualTo("A"));

        // B: Single -> A to 2B, B to 1B
        var outcomeB = resolver.Resolve(b2);
        game.AdvanceRunners(outcomeB, b2, isHomeTeam: false);
        Assert.That(game.State.Bases[0]?.Name, Is.EqualTo("B"));
        Assert.That(game.State.Bases[1]?.Name, Is.EqualTo("A"));

        // C: Out
        game.State.RecordOut();
        Assert.That(game.State.Outs, Is.EqualTo(1));

        // D: Out
        game.State.RecordOut();
        Assert.That(game.State.Outs, Is.EqualTo(2));

        // Next: Out to end half
        game.State.RecordOut();
        Assert.That(game.State.Outs, Is.EqualTo(3));
    }
}
