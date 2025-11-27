using System;
using DiamondX.Core.Models;

namespace DiamondX.Core.State;

public enum HalfInning
{
    Top,
    Bottom
}

public class GameState
{
    private readonly Player?[] _bases = new Player?[3];

    public int Inning { get; private set; } = 1;
    public HalfInning Half { get; private set; } = HalfInning.Top;
    public int Outs { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }

    public IReadOnlyList<Player?> Bases => _bases;

    public void BeginHalfInning(int inning, HalfInning half)
    {
        Inning = inning;
        Half = half;
        Outs = 0;
        Array.Clear(_bases, 0, _bases.Length);
    }

    public void RecordOut()
    {
        if (Outs >= 3)
        {
            throw new InvalidOperationException("Cannot record more than three outs in a half inning.");
        }

        Outs++;
    }

    public void AddRun(bool isHomeTeam)
    {
        if (isHomeTeam)
        {
            HomeScore++;
        }
        else
        {
            AwayScore++;
        }
    }

    public Player? GetBase(int index) => _bases[index];

    public void SetBase(int index, Player? player)
    {
        _bases[index] = player;
    }
}
