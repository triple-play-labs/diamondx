// Player.cs

namespace DiamondX;

public class Player
{
    public string Name { get; }
    // Rates are per plate appearance
    public double WalkRate { get; }
    public double SingleRate { get; }
    public double DoubleRate { get; }
    public double TripleRate { get; }
    public double HomeRunRate { get; }

    public Player(string name, double walkRate, double singleRate, double doubleRate, double tripleRate, double homeRunRate)
    {
        // A simple validation to ensure total probability is not over 100%
        if (walkRate + singleRate + doubleRate + tripleRate + homeRunRate > 1.0)
        {
            throw new ArgumentException("The sum of outcome rates cannot exceed 1.0.");
        }

        Name = name;
        WalkRate = walkRate;
        SingleRate = singleRate;
        DoubleRate = doubleRate;
        TripleRate = tripleRate;
        HomeRunRate = homeRunRate;
    }

    public override string ToString()
        => $"Player({Name})";
}
