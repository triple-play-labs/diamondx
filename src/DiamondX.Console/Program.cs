// Program.cs

using System.Diagnostics;
using DiamondX.Core;
using DiamondX.Core.Models;
using DiamondX.Core.Simulation;
using DiamondX.Weather;
using SimulationEngine.Core;
using SimulationEngine.Events;
using SimulationEngine.Orchestration;
using SimulationEngine.Random;
using SimulationEngine.State;
using SimulationEngine.Time;

// Suppress SonarQube string literal warning for console output
#pragma warning disable S1192
// Suppress cognitive complexity warning - this is a CLI entry point
#pragma warning disable S3776

// Check command line args for simulation mode
bool runMonteCarlo = args.Contains("--monte-carlo") || args.Contains("-mc");
bool runOrchestrated = args.Contains("--orchestrated") || args.Contains("-o");
int numSimulations = ParseSimulationCount(args);

// Show help if requested
if (args.Contains("--help") || args.Contains("-h"))
{
    ShowHelp();
    return;
}

static int ParseSimulationCount(string[] args)
{
    // Parse custom count first (overrides presets)
    var simCountArg = args.FirstOrDefault(a => a.StartsWith("--count=") || a.StartsWith("-n="));
    if (simCountArg != null)
    {
        var countStr = simCountArg.Split('=')[1];
        if (int.TryParse(countStr, out int count))
            return count;
    }

    // Parse presets
    if (args.Contains("--single") || args.Contains("-1"))
        return 1;
    if (args.Contains("--season") || args.Contains("-162"))
        return 162;

    return 10000; // Default
}

static void ShowHelp()
{
    Console.WriteLine("DiamondX Baseball Simulator");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  (no args)        Run a single verbose game");
    Console.WriteLine("  -mc, --monte-carlo  Run Monte Carlo simulations");
    Console.WriteLine("  -o, --orchestrated  Run multi-model Weather+Baseball demo");
    Console.WriteLine();
    Console.WriteLine("Simulation count (with -mc):");
    Console.WriteLine("  -1, --single     Run 1 simulation");
    Console.WriteLine("  -162, --season   Run 162 simulations (one season)");
    Console.WriteLine("  -10k, --full     Run 10,000 simulations (default)");
    Console.WriteLine("  -n=N, --count=N  Run N simulations");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run                    # Single verbose game");
    Console.WriteLine("  dotnet run -mc -1             # One silent simulation");
    Console.WriteLine("  dotnet run -mc --season       # 162-game season sim");
    Console.WriteLine("  dotnet run -mc -n=50000       # 50,000 simulations");
    Console.WriteLine("  dotnet run -o                 # Weather + Baseball orchestrated");
}

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

// Pitchers with 2023-ish stats (rates are per plate appearance)
// Format: new Pitcher(Name, WalkRate, SinglesAllowed, DoublesAllowed, TriplesAllowed, HRAllowed, StrikeoutRate, FatigueThreshold, MaxPitchCount)

// Giants starter - Logan Webb (workhorse, higher fatigue threshold)
var giantsPitcher = new Pitcher(
    name: "Logan Webb",
    walkRate: 0.055,
    singlesAllowedRate: 0.145,
    doublesAllowedRate: 0.040,
    triplesAllowedRate: 0.003,
    homeRunsAllowedRate: 0.022,
    strikeoutRate: 0.200,
    fatigueThreshold: 85,
    maxPitchCount: 115
);

// Dodgers starter - Clayton Kershaw (elite but lower durability)
var dodgersPitcher = new Pitcher(
    name: "Clayton Kershaw",
    walkRate: 0.050,
    singlesAllowedRate: 0.138,
    doublesAllowedRate: 0.038,
    triplesAllowedRate: 0.002,
    homeRunsAllowedRate: 0.028,
    strikeoutRate: 0.245,
    fatigueThreshold: 70,
    maxPitchCount: 100
);

if (runMonteCarlo)
{
    RunMonteCarloSimulation(
        giantsLineup, dodgersLineup,
        giantsPitcher, dodgersPitcher,
        numSimulations);
}
else if (runOrchestrated)
{
    RunOrchestratedDemo(
        giantsLineup, dodgersLineup,
        giantsPitcher, dodgersPitcher);
}
else
{
    // Create and play a single game with verbose output
    var simGame = new Game(
        homeTeam: giantsLineup,
        awayTeam: dodgersLineup,
        homeTeamName: "Giants",
        awayTeamName: "Dodgers",
        homePitcher: giantsPitcher,
        awayPitcher: dodgersPitcher);
    simGame.PlayGame();
}

/// <summary>
/// Runs Monte Carlo simulations using the SimulationEngine to compute win probabilities.
/// </summary>
static void RunMonteCarloSimulation(
    List<Player> homeTeam,
    List<Player> awayTeam,
    Pitcher homePitcher,
    Pitcher awayPitcher,
    int numSimulations)
{
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║       ⚾ DiamondX Monte Carlo Win Probability Calculator ⚾    ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"  Matchup: Dodgers @ Giants");
    Console.WriteLine($"  Pitchers: {awayPitcher.Name} vs {homePitcher.Name}");
    Console.WriteLine($"  Simulations: {numSimulations:N0}");
    Console.WriteLine();

    var config = new GameConfig
    {
        HomeTeam = homeTeam,
        AwayTeam = awayTeam,
        HomeTeamName = "Giants",
        AwayTeamName = "Dodgers",
        HomePitcher = homePitcher,
        AwayPitcher = awayPitcher
    };

    // Set up parameters for silent mode
    var parameters = new SimulationParameters();
    parameters.Set("verbose", false);

    var stopwatch = Stopwatch.StartNew();

    // Collect results - we run simulations directly to capture game state
    Console.WriteLine("  Running simulations...");
    var baseSeed = new Random(42);
    var seeds = Enumerable.Range(0, numSimulations).Select(_ => baseSeed.Next()).ToArray();

    // Result accumulators
    var homeWins = 0;
    var awayWins = 0;
    var totalHomeRuns = 0;
    var totalAwayRuns = 0;
    var extraInningGames = 0;
    var homeScores = new List<int>(numSimulations);
    var awayScores = new List<int>(numSimulations);
    var totalInnings = new List<int>(numSimulations);
    var lockObj = new object();

    // Use raw Parallel.For to run simulations and capture state
    Parallel.For(0, numSimulations, i =>
    {
        var simulation = new BaseballGameSimulation(config);

        // Create a simple context to run the simulation
        var random = new SeedableRandomSource(seeds[i]);
        var clock = new SimulationClock();
        var events = new EventScheduler();
        var state = new InMemoryStateManager(() => (clock.TickCount, clock.CurrentTime));
        var metrics = new SimulationMetrics(Guid.NewGuid());
        var ctx = new SimulationContext(Guid.NewGuid(), random, clock, events, state, parameters, metrics);

        simulation.Initialize(ctx);

        // Run to completion
        while (!simulation.IsComplete)
        {
            simulation.Step();
            if (ctx.Clock.TickCount > 500) break; // Safety limit
        }

        // Capture results
        var game = simulation.Game!;
        var homeScore = game.HomeScore;
        var awayScore = game.AwayScore;
        var innings = game.CurrentInning;

        lock (lockObj)
        {
            if (homeScore > awayScore)
                homeWins++;
            else
                awayWins++;

            totalHomeRuns += homeScore;
            totalAwayRuns += awayScore;
            homeScores.Add(homeScore);
            awayScores.Add(awayScore);
            totalInnings.Add(innings);

            if (innings > 9)
                extraInningGames++;
        }
    });

    stopwatch.Stop();

    // Calculate statistics
    double homeWinPct = (double)homeWins / numSimulations * 100;
    double awayWinPct = (double)awayWins / numSimulations * 100;
    double avgHomeScore = (double)totalHomeRuns / numSimulations;
    double avgAwayScore = (double)totalAwayRuns / numSimulations;
    double avgInnings = totalInnings.Average();
    double extraInningPct = (double)extraInningGames / numSimulations * 100;

    // Sort for percentiles
    homeScores.Sort();
    awayScores.Sort();

    int p10Home = homeScores[(int)(numSimulations * 0.10)];
    int p50Home = homeScores[(int)(numSimulations * 0.50)];
    int p90Home = homeScores[(int)(numSimulations * 0.90)];
    int p10Away = awayScores[(int)(numSimulations * 0.10)];
    int p50Away = awayScores[(int)(numSimulations * 0.50)];
    int p90Away = awayScores[(int)(numSimulations * 0.90)];

    // Display results
    Console.WriteLine();
    Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│                        SIMULATION RESULTS                       │");
    Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│  Win Probability:                                              │");
    Console.WriteLine($"│    Giants (Home): {homeWinPct,6:F2}%  ({homeWins:N0} wins)                       │");
    Console.WriteLine($"│    Dodgers (Away): {awayWinPct,6:F2}%  ({awayWins:N0} wins)                       │");
    Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│  Average Score:                                                │");
    Console.WriteLine($"│    Giants: {avgHomeScore:F2} runs/game                                      │");
    Console.WriteLine($"│    Dodgers: {avgAwayScore:F2} runs/game                                      │");
    Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│  Score Distribution (10th / 50th / 90th percentile):           │");
    Console.WriteLine($"│    Giants: {p10Home} / {p50Home} / {p90Home} runs                                       │");
    Console.WriteLine($"│    Dodgers: {p10Away} / {p50Away} / {p90Away} runs                                       │");
    Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│  Game Length:                                                  │");
    Console.WriteLine($"│    Average innings: {avgInnings:F2}                                         │");
    Console.WriteLine($"│    Extra inning games: {extraInningPct:F1}%                                     │");
    Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│  Performance:                                                  │");
    Console.WriteLine($"│    Time elapsed: {stopwatch.ElapsedMilliseconds:N0} ms                                         │");
    Console.WriteLine($"│    Simulations/sec: {numSimulations / stopwatch.Elapsed.TotalSeconds:N0}                                  │");
    Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
}

/// <summary>
/// Runs a multi-model orchestrated simulation with Weather affecting Baseball outcomes.
/// </summary>
static void RunOrchestratedDemo(
    List<Player> homeTeam,
    List<Player> awayTeam,
    Pitcher homePitcher,
    Pitcher awayPitcher)
{
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     ⚾🌤️ DiamondX Multi-Model Orchestration Demo 🌤️⚾       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("  This demo shows Weather and Baseball models running together,");
    Console.WriteLine("  with weather conditions affecting game play in real-time.");
    Console.WriteLine();

    // Create the orchestrator
    var orchestrator = new SimulationOrchestrator();

    // Create weather simulation with "windy day" conditions
    var weatherConfig = WeatherConfig.WindyDay();
    var weatherSim = new WeatherSimulation(weatherConfig);

    // Create baseball game simulation
    var gameConfig = new GameConfig
    {
        HomeTeam = homeTeam,
        AwayTeam = awayTeam,
        HomeTeamName = "Giants",
        AwayTeamName = "Dodgers",
        HomePitcher = homePitcher,
        AwayPitcher = awayPitcher
    };
    var baseballSim = new BaseballGameSimulation(gameConfig);

    // Register models - Weather runs first (priority 100), Baseball depends on it
    orchestrator.Register(weatherSim, new ModelOptions { Priority = 100 });
    orchestrator.Register(baseballSim, new ModelOptions { Priority = 50 }, "Weather");

    // Set up simulation infrastructure
    var random = new SeedableRandomSource(42);
    var clock = new SimulationClock();
    var events = new EventScheduler();
    var state = new InMemoryStateManager(() => (clock.TickCount, clock.CurrentTime));
    var parameters = new SimulationParameters();
    parameters.Set("verbose", false);
    var metrics = new SimulationMetrics(Guid.NewGuid());
    var ctx = new SimulationContext(Guid.NewGuid(), random, clock, events, state, parameters, metrics);

    // Initialize all models
    orchestrator.Initialize(ctx);

    Console.WriteLine($"  Initial Weather: {weatherSim.Current.GetDescription()}");
    Console.WriteLine($"  HR Modifier: {weatherSim.Current.GetHomeRunModifier():+0.0;-0.0;0} feet");
    Console.WriteLine();
    Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│  Inning │ Score       │ Weather                    │ HR Mod  │");
    Console.WriteLine("├────────────────────────────────────────────────────────────────┤");

    var lastInning = 0;
    var stepCount = 0;
    const int maxSteps = 500;

    // Run the orchestrated simulation
    while (!baseballSim.IsComplete && stepCount < maxSteps)
    {
        orchestrator.Step();
        stepCount++;

        // Print status when inning changes
        var currentInning = baseballSim.Game?.CurrentInning ?? 0;
        if (currentInning != lastInning && baseballSim.Game != null)
        {
            var game = baseballSim.Game;
            var weather = weatherSim.Current;
            var inningStr = $"{currentInning}".PadLeft(2);
            var scoreStr = $"DOD {game.AwayScore} - SF {game.HomeScore}";
            var weatherDesc = GetShortWeatherDesc(weather);
            var hrMod = weather.GetHomeRunModifier();

            Console.WriteLine($"│    {inningStr}   │ {scoreStr,-11} │ {weatherDesc,-26} │ {hrMod,+6:+0.0;-0.0;0}ft │");
            lastInning = currentInning;
        }
    }

    Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
    Console.WriteLine();

    // Final results
    if (baseballSim.Game != null)
    {
        var game = baseballSim.Game;
        var finalWeather = weatherSim.Current;

        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                          FINAL RESULT                          │");
        Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│  Dodgers: {game.AwayScore}                                                      │");
        Console.WriteLine($"│  Giants:  {game.HomeScore}                                                      │");
        Console.WriteLine($"│  Winner:  {(game.HomeScore > game.AwayScore ? "Giants" : "Dodgers"),-10}                                           │");
        Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│  Final Weather: {finalWeather.GetDescription(),-40}   │");
        Console.WriteLine($"│  Simulation Steps: {stepCount,-5}                                        │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
    }

    orchestrator.Dispose();
}

static string GetShortWeatherDesc(WeatherConditions w)
{
    var wind = w.WindSpeed >= 15 ? $"💨{w.WindSpeed:F0}mph" : "";
    var temp = $"{w.Temperature:F0}°F";
    var sky = w.Sky switch
    {
        SkyCondition.Clear => "☀️",
        SkyCondition.PartlyCloudy => "⛅",
        SkyCondition.Cloudy => "☁️",
        SkyCondition.Overcast => "🌥️",
        _ => ""
    };
    var precip = w.Precipitation switch
    {
        PrecipitationType.Drizzle => "🌧️",
        PrecipitationType.Light => "🌧️",
        PrecipitationType.Moderate => "🌧️🌧️",
        PrecipitationType.Heavy => "⛈️",
        PrecipitationType.Thunderstorm => "⛈️⚡",
        _ => ""
    };

    return $"{sky} {temp} {wind} {precip}".Trim();
}