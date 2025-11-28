using DiamondX.Core.Models;
using DiamondX.Core.State;
using SimulationEngine.Core;
using SimulationEngine.Random;

namespace DiamondX.Core.Simulation;

/// <summary>
/// Adapter that wraps a baseball Game to implement ISimulation,
/// allowing it to be executed by the SimulationEngine.
/// </summary>
public sealed class BaseballGameSimulation : ISimulation
{
    private readonly GameConfig _config;
    private Game? _game;
    private ISimulationContext? _context;

    public string Name => "BaseballGame";
    public string Version => "1.0.0";
    public bool IsComplete => _game?.IsGameOver ?? false;

    /// <summary>
    /// Gets the underlying game instance (available after Initialize).
    /// </summary>
    public Game? Game => _game;

    /// <summary>
    /// Creates a baseball simulation with the specified configuration.
    /// </summary>
    public BaseballGameSimulation(GameConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Creates a baseball simulation with team lineups.
    /// </summary>
    public BaseballGameSimulation(
        List<Player> homeTeam,
        List<Player> awayTeam,
        string homeTeamName = "Home",
        string awayTeamName = "Away",
        Pitcher? homePitcher = null,
        Pitcher? awayPitcher = null)
        : this(new GameConfig
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            HomeTeamName = homeTeamName,
            AwayTeamName = awayTeamName,
            HomePitcher = homePitcher,
            AwayPitcher = awayPitcher
        })
    {
    }

    public void Initialize(ISimulationContext context)
    {
        _context = context;

        // Check for verbose parameter
        bool verbose = context.Parameters.GetOrDefault("verbose", false);

        // Create plate appearance resolver using the engine's RNG for reproducibility
        var resolver = new PlateAppearanceResolver(context.Random);

        // Create the game with engine-provided RNG
        _game = new Game(
            homeTeam: _config.HomeTeam,
            awayTeam: _config.AwayTeam,
            homeTeamName: _config.HomeTeamName,
            awayTeamName: _config.AwayTeamName,
            homePitcher: _config.HomePitcher,
            awayPitcher: _config.AwayPitcher,
            plateAppearanceResolver: resolver,
            verbose: verbose
        );
    }

    public SimulationStepResult Step()
    {
        if (_game is null)
            throw new InvalidOperationException("Simulation not initialized. Call Initialize first.");

        // Execute one plate appearance
        _game.PlayPlateAppearance();

        // Advance simulation clock by approximate PA duration (30 seconds)
        _context?.Clock.Advance(TimeSpan.FromSeconds(30));

        return _game.IsGameOver ? SimulationStepResult.Completed : SimulationStepResult.Continue;
    }

    public void Dispose()
    {
        _game = null;
        _context = null;
    }
}

/// <summary>
/// Configuration for creating a baseball game simulation.
/// </summary>
public class GameConfig
{
    public required List<Player> HomeTeam { get; init; }
    public required List<Player> AwayTeam { get; init; }
    public string HomeTeamName { get; init; } = "Home";
    public string AwayTeamName { get; init; } = "Away";
    public Pitcher? HomePitcher { get; init; }
    public Pitcher? AwayPitcher { get; init; }
}

/// <summary>
/// Result of a completed baseball game simulation.
/// </summary>
public class BaseballGameResult
{
    public required int HomeScore { get; init; }
    public required int AwayScore { get; init; }
    public required string HomeTeamName { get; init; }
    public required string AwayTeamName { get; init; }
    public required int TotalInnings { get; init; }
    public required int TotalPlateAppearances { get; init; }
    public required bool HomeWin { get; init; }

    public static BaseballGameResult FromGame(Game game)
    {
        return new BaseballGameResult
        {
            HomeScore = game.HomeScore,
            AwayScore = game.AwayScore,
            HomeTeamName = game.HomeTeamName,
            AwayTeamName = game.AwayTeamName,
            TotalInnings = game.CurrentInning,
            TotalPlateAppearances = game.TotalPlateAppearances,
            HomeWin = game.HomeScore > game.AwayScore
        };
    }
}
