using DiamondX.Core.Models;

namespace DiamondX.Core.State;

/// <summary>
/// Represents whether we're in the top or bottom of an inning.
/// </summary>
public enum HalfInning
{
    Top,
    Bottom
}

/// <summary>
/// Centralized game state for the baseball simulation.
/// Provides a single source of truth for inning, bases, outs, and scores.
/// </summary>
public class GameState
{
    private readonly Player?[] _bases = new Player?[3];

    public int Inning { get; private set; } = 1;
    public HalfInning Half { get; private set; } = HalfInning.Top;
    public int Outs { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }

    /// <summary>
    /// Read-only view of baserunners. Index 0 = 1st, 1 = 2nd, 2 = 3rd.
    /// </summary>
    public IReadOnlyList<Player?> Bases => _bases;

    public void BeginHalfInning(int inning, HalfInning half)
    {
        Inning = inning;
        Half = half;
        Outs = 0;
        ClearBases();
    }

    public void RecordOut()
    {
        if (Outs >= 3)
            throw new InvalidOperationException("Cannot record more than 3 outs in a half-inning.");
        Outs++;
    }

    public void AddRun(bool isHomeTeam)
    {
        if (isHomeTeam)
            HomeScore++;
        else
            AwayScore++;
    }

    public Player? GetBase(int index)
    {
        if (index < 0 || index >= 3)
            throw new ArgumentOutOfRangeException(nameof(index), "Base index must be 0, 1, or 2.");
        return _bases[index];
    }

    public void SetBase(int index, Player? runner)
    {
        if (index < 0 || index >= 3)
            throw new ArgumentOutOfRangeException(nameof(index), "Base index must be 0, 1, or 2.");
        _bases[index] = runner;
    }

    public void ClearBases()
    {
        Array.Clear(_bases, 0, _bases.Length);
    }

    public bool AreBasesLoaded => _bases[0] != null && _bases[1] != null && _bases[2] != null;
    public bool IsBaseEmpty(int index) => GetBase(index) == null;
}
