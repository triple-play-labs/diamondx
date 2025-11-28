using SimulationEngine.Core;
using SimulationEngine.Random;
using SimulationEngine.Time;

namespace DiamondX.Tests;

/// <summary>
/// Tests for the core simulation engine components.
/// </summary>
public class SimulationEngineTests
{
    #region SimulationClock Tests

    [Test]
    public void SimulationClock_StartsAtZero()
    {
        var clock = new SimulationClock();

        Assert.That(clock.CurrentTime, Is.EqualTo(TimeSpan.Zero));
        Assert.That(clock.TickCount, Is.EqualTo(0));
    }

    [Test]
    public void SimulationClock_Advance_IncreasesTimeAndTicks()
    {
        var clock = new SimulationClock();

        clock.Advance(TimeSpan.FromSeconds(5));

        Assert.That(clock.CurrentTime, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(clock.TickCount, Is.EqualTo(1));
    }

    [Test]
    public void SimulationClock_Advance_ThrowsForNegativeDelta()
    {
        var clock = new SimulationClock();

        Assert.Throws<ArgumentException>(() => clock.Advance(TimeSpan.FromSeconds(-1)));
    }

    [Test]
    public void SimulationClock_SetTime_UpdatesCurrentTime()
    {
        var clock = new SimulationClock();

        clock.SetTime(TimeSpan.FromMinutes(10));

        Assert.That(clock.CurrentTime, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }

    [Test]
    public void SimulationClock_SetTime_ThrowsForBackwardTime()
    {
        var clock = new SimulationClock();
        clock.SetTime(TimeSpan.FromMinutes(10));

        Assert.Throws<InvalidOperationException>(() => clock.SetTime(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void SimulationClock_FixedStepMode_AdvancesTimeOnTick()
    {
        var stepSize = TimeSpan.FromMilliseconds(100);
        var clock = new SimulationClock(ClockMode.FixedStep, stepSize);

        clock.Tick();
        clock.Tick();
        clock.Tick();

        Assert.That(clock.CurrentTime, Is.EqualTo(TimeSpan.FromMilliseconds(300)));
        Assert.That(clock.TickCount, Is.EqualTo(3));
    }

    [Test]
    public void SimulationClock_Snapshot_CapturesAndRestoresState()
    {
        var clock = new SimulationClock();
        clock.Advance(TimeSpan.FromSeconds(30));
        clock.Tick();

        var snapshot = clock.CreateSnapshot();
        clock.Advance(TimeSpan.FromSeconds(60));

        clock.RestoreSnapshot(snapshot);

        Assert.That(clock.CurrentTime, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(clock.TickCount, Is.EqualTo(2)); // Tick was called once before snapshot
    }

    [Test]
    public void SimulationClock_Reset_ClearsState()
    {
        var clock = new SimulationClock();
        clock.Advance(TimeSpan.FromMinutes(5));
        clock.Tick();

        clock.Reset();

        Assert.That(clock.CurrentTime, Is.EqualTo(TimeSpan.Zero));
        Assert.That(clock.TickCount, Is.EqualTo(0));
    }

    #endregion

    #region SeedableRandomSource Tests

    [Test]
    public void SeedableRandomSource_SameSeed_ProducesSameSequence()
    {
        var rng1 = new SeedableRandomSource(12345);
        var rng2 = new SeedableRandomSource(12345);

        var values1 = Enumerable.Range(0, 100).Select(_ => rng1.NextDouble()).ToList();
        var values2 = Enumerable.Range(0, 100).Select(_ => rng2.NextDouble()).ToList();

        Assert.That(values1, Is.EqualTo(values2));
    }

    [Test]
    public void SeedableRandomSource_DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new SeedableRandomSource(12345);
        var rng2 = new SeedableRandomSource(54321);

        var values1 = Enumerable.Range(0, 10).Select(_ => rng1.NextDouble()).ToList();
        var values2 = Enumerable.Range(0, 10).Select(_ => rng2.NextDouble()).ToList();

        Assert.That(values1, Is.Not.EqualTo(values2));
    }

    [Test]
    public void SeedableRandomSource_TracksGenerationCount()
    {
        var rng = new SeedableRandomSource(42);

        Assert.That(rng.GenerationCount, Is.EqualTo(0));

        rng.NextDouble();
        rng.NextDouble();
        rng.NextInt(10);

        Assert.That(rng.GenerationCount, Is.EqualTo(3));
    }

    [Test]
    public void SeedableRandomSource_NextInt_ReturnsValueInRange()
    {
        var rng = new SeedableRandomSource(42);

        for (int i = 0; i < 100; i++)
        {
            var value = rng.NextInt(10, 20);
            Assert.That(value, Is.GreaterThanOrEqualTo(10).And.LessThan(20));
        }
    }

    [Test]
    public void SeedableRandomSource_NextBool_RespectsProbaility()
    {
        var rng = new SeedableRandomSource(42);
        var trueCount = 0;
        var iterations = 10000;

        for (int i = 0; i < iterations; i++)
        {
            if (rng.NextBool(0.3)) trueCount++;
        }

        // Should be approximately 30% true (with some tolerance)
        var ratio = (double)trueCount / iterations;
        Assert.That(ratio, Is.InRange(0.25, 0.35));
    }

    [Test]
    public void SeedableRandomSource_Choose_SelectsFromCollection()
    {
        var rng = new SeedableRandomSource(42);
        var items = new[] { "A", "B", "C", "D" };

        var chosen = rng.Choose(items);

        Assert.That(items, Does.Contain(chosen));
    }

    [Test]
    public void SeedableRandomSource_Choose_ThrowsOnEmptyCollection()
    {
        var rng = new SeedableRandomSource(42);
        var items = Array.Empty<string>();

        Assert.Throws<ArgumentException>(() => rng.Choose(items));
    }

    #endregion

    #region SimulationParameters Tests

    [Test]
    public void SimulationParameters_SetAndGet_Works()
    {
        var parameters = new SimulationParameters()
            .Set("iterations", 1000)
            .Set("name", "test")
            .Set("enabled", true);

        Assert.That(parameters.Get<int>("iterations"), Is.EqualTo(1000));
        Assert.That(parameters.Get<string>("name"), Is.EqualTo("test"));
        Assert.That(parameters.Get<bool>("enabled"), Is.True);
    }

    [Test]
    public void SimulationParameters_Get_ThrowsForMissingKey()
    {
        var parameters = new SimulationParameters();

        Assert.Throws<KeyNotFoundException>(() => parameters.Get<int>("missing"));
    }

    [Test]
    public void SimulationParameters_TryGet_ReturnsFalseForMissingKey()
    {
        var parameters = new SimulationParameters();

        var found = parameters.TryGet<int>("missing", out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void SimulationParameters_GetOrDefault_ReturnsDefaultForMissingKey()
    {
        var parameters = new SimulationParameters();

        var value = parameters.GetOrDefault("missing", 42);

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void SimulationParameters_Contains_Works()
    {
        var parameters = new SimulationParameters()
            .Set("exists", true);

        Assert.That(parameters.Contains("exists"), Is.True);
        Assert.That(parameters.Contains("missing"), Is.False);
    }

    [Test]
    public void SimulationParameters_GetKeys_ReturnsAllKeys()
    {
        var parameters = new SimulationParameters()
            .Set("a", 1)
            .Set("b", 2)
            .Set("c", 3);

        var keys = parameters.GetKeys().ToList();

        Assert.That(keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
    }

    #endregion

    #region SimulationRunner Tests

    [Test]
    public void SimulationRunner_RunsSimpleSimulationToCompletion()
    {
        var runner = new SimulationRunner();
        var simulation = new CountingSimulation(10);

        var result = runner.Run(simulation);

        Assert.That(result.Status, Is.EqualTo(SimulationStatus.Completed));
        Assert.That(result.Metrics.StepCount, Is.EqualTo(10));
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void SimulationRunner_RespectsMaxSteps()
    {
        var options = new SimulationRunnerOptions { MaxSteps = 5 };
        var runner = new SimulationRunner(options);
        var simulation = new CountingSimulation(100);

        var result = runner.Run(simulation);

        Assert.That(result.Status, Is.EqualTo(SimulationStatus.Stopped));
        Assert.That(result.Metrics.StepCount, Is.EqualTo(5));
    }

    [Test]
    public void SimulationRunner_SameSeedProducesSameResult()
    {
        var runner = new SimulationRunner();
        var seed = 12345;

        var sim1 = new RandomOutcomeSimulation(100);
        var sim2 = new RandomOutcomeSimulation(100);

        var result1 = runner.Run(sim1, seed);
        var result2 = runner.Run(sim2, seed);

        Assert.That(result1.Seed, Is.EqualTo(result2.Seed));
        Assert.That(sim1.Sum, Is.EqualTo(sim2.Sum));
    }

    [Test]
    public void SimulationRunner_ParallelRuns_CompleteAllSimulations()
    {
        var runner = new SimulationRunner();
        var runCount = 10;

        var results = runner.RunParallel(
            () => new CountingSimulation(5),
            runCount
        );

        Assert.That(results.Count, Is.EqualTo(runCount));
        Assert.That(results.All(r => r.IsSuccess), Is.True);
        Assert.That(results.Select(r => r.Seed).Distinct().Count(), Is.EqualTo(runCount)); // All different seeds
    }

    [Test]
    public void SimulationRunner_ParallelRuns_WithBaseSeedAreReproducible()
    {
        var runner = new SimulationRunner();
        var baseSeed = 42;

        var results1 = runner.RunParallel(() => new CountingSimulation(5), 5, null, baseSeed);
        var results2 = runner.RunParallel(() => new CountingSimulation(5), 5, null, baseSeed);

        var seeds1 = results1.Select(r => r.Seed).ToList();
        var seeds2 = results2.Select(r => r.Seed).ToList();

        Assert.That(seeds1, Is.EqualTo(seeds2));
    }

    #endregion

    #region Test Simulation Implementations

    /// <summary>
    /// Simple simulation that counts to a target.
    /// </summary>
    private sealed class CountingSimulation : ISimulation
    {
        private readonly int _target;
        private int _count;

        public string Name => "CountingSimulation";
        public string Version => "1.0";
        public bool IsComplete => _count >= _target;

        public CountingSimulation(int target)
        {
            _target = target;
        }

        public void Initialize(ISimulationContext context) { }

        public SimulationStepResult Step()
        {
            _count++;
            return _count >= _target ? SimulationStepResult.Completed : SimulationStepResult.Continue;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Simulation that uses random numbers to test seed reproducibility.
    /// </summary>
    private sealed class RandomOutcomeSimulation : ISimulation
    {
        private readonly int _steps;
        private int _currentStep;
        private ISimulationContext? _context;

        public string Name => "RandomOutcomeSimulation";
        public string Version => "1.0";
        public bool IsComplete => _currentStep >= _steps;
        public double Sum { get; private set; }

        public RandomOutcomeSimulation(int steps)
        {
            _steps = steps;
        }

        public void Initialize(ISimulationContext context)
        {
            _context = context;
        }

        public SimulationStepResult Step()
        {
            Sum += _context!.Random.NextDouble();
            _currentStep++;
            return _currentStep >= _steps ? SimulationStepResult.Completed : SimulationStepResult.Continue;
        }

        public void Dispose() { }
    }

    #endregion
}
