// Program.cs

using DiamondX.Core;
using DiamondX.Core.Models;

// Approximate 2023 stats per plate appearance.
// Format: new Player(Name, Walk%, Single%, Double%, Triple%, HomeRun%)

// San Francisco Giants
var giantsLineup = new List<Player>
{
    new("LaMonte Wade Jr.", 0.124, 0.153, 0.041, 0.004, 0.033),
    new("Thairo Estrada", 0.048, 0.201, 0.050, 0.002, 0.027),
    new("Wilmer Flores", 0.084, 0.177, 0.051, 0.002, 0.054),
    new("J.D. Davis", 0.086, 0.142, 0.043, 0.002, 0.035),
    new("Michael Conforto", 0.106, 0.131, 0.039, 0.002, 0.036),
    new("Mike Yastrzemski", 0.096, 0.106, 0.049, 0.005, 0.037),
    new("Patrick Bailey", 0.071, 0.165, 0.052, 0.003, 0.021),
    new("Casey Schmitt", 0.035, 0.161, 0.038, 0.000, 0.006),
    new("Brandon Crawford", 0.086, 0.131, 0.038, 0.003, 0.021),
};

// Los Angeles Dodgers
var dodgersLineup = new List<Player>
{
    new("Mookie Betts", 0.137, 0.163, 0.060, 0.001, 0.057),
    new("Freddie Freeman", 0.131, 0.187, 0.082, 0.003, 0.041),
    new("Will Smith", 0.107, 0.162, 0.042, 0.002, 0.037),
    new("J.D. Martinez", 0.078, 0.142, 0.055, 0.004, 0.068),
    new("Max Muncy", 0.141, 0.098, 0.030, 0.002, 0.069),
    new("Jason Heyward", 0.090, 0.161, 0.053, 0.005, 0.036),
    new("James Outman", 0.101, 0.131, 0.043, 0.006, 0.043),
    new("David Peralta", 0.054, 0.198, 0.048, 0.000, 0.016),
    new("Miguel Rojas", 0.065, 0.183, 0.037, 0.003, 0.012),
};

// Create and play the game
var simGame = new Game(homeTeam: giantsLineup, awayTeam: dodgersLineup);
simGame.PlayGame();