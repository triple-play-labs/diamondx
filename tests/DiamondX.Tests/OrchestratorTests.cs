using NUnit.Framework;
using SimulationEngine.Core;
using SimulationEngine.Orchestration;
using SimulationEngine.Events;
using SimulationEngine.Random;
using SimulationEngine.State;
using SimulationEngine.Time;

namespace DiamondX.Tests;

[TestFixture]
public class OrchestratorTests
{
    private SimulationOrchestrator _orchestrator = null!;
    private ISimulationContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _orchestrator = new SimulationOrchestrator();
        _context = CreateTestContext();
    }

    [TearDown]
    public void TearDown()
    {
        _orchestrator.Dispose();
    }

    private static ISimulationContext CreateTestContext()
    {
        var runId = Guid.NewGuid();
        var random = new SeedableRandomSource(42);
        var clock = new SimulationClock();
        var events = new EventScheduler();
        var state = new InMemoryStateManager(() => (clock.TickCount, clock.CurrentTime));
        var parameters = new SimulationParameters();
        var metrics = new SimulationMetrics(runId);

        return new SimulationContext(runId, random, clock, events, state, parameters, metrics);
    }

    [Test]
    public void Register_SingleModel_AddsToModels()
    {
        var model = new TestSimulation("Model1", stepsToComplete: 5);

        var registration = _orchestrator.Register(model);

        Assert.That(registration.Id, Is.EqualTo("Model1"));
        Assert.That(registration.Model, Is.SameAs(model));
        Assert.That(_orchestrator.State, Is.EqualTo(OrchestratorState.Ready));
    }

    [Test]
    public void Register_DuplicateId_ThrowsException()
    {
        var model1 = new TestSimulation("Model1", stepsToComplete: 5);
        var model2 = new TestSimulation("Model1", stepsToComplete: 3);

        _orchestrator.Register(model1);

        Assert.Throws<ArgumentException>(() => _orchestrator.Register(model2));
    }

    [Test]
    public void Initialize_SetsStateToRunning()
    {
        var model = new TestSimulation("Model1", stepsToComplete: 5);
        _orchestrator.Register(model);

        _orchestrator.Initialize(_context);

        Assert.That(_orchestrator.State, Is.EqualTo(OrchestratorState.Running));
        Assert.That(model.WasInitialized, Is.True);
    }

    [Test]
    public void Initialize_WithNoModels_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => _orchestrator.Initialize(_context));
    }

    [Test]
    public void Step_ExecutesAllModels()
    {
        var model1 = new TestSimulation("Model1", stepsToComplete: 5);
        var model2 = new TestSimulation("Model2", stepsToComplete: 5);
        _orchestrator.Register(model1);
        _orchestrator.Register(model2);
        _orchestrator.Initialize(_context);

        var result = _orchestrator.Step();

        Assert.That(result, Is.EqualTo(OrchestratorStepResult.Continue));
        Assert.That(model1.StepsCalled, Is.EqualTo(1));
        Assert.That(model2.StepsCalled, Is.EqualTo(1));
    }

    [Test]
    public void Step_CompletesWhenAllModelsComplete()
    {
        var model = new TestSimulation("Model1", stepsToComplete: 2);
        _orchestrator.Register(model);
        _orchestrator.Initialize(_context);

        _orchestrator.Step();
        _orchestrator.Step();
        var result = _orchestrator.Step();

        Assert.That(result, Is.EqualTo(OrchestratorStepResult.Completed));
        Assert.That(_orchestrator.State, Is.EqualTo(OrchestratorState.Completed));
    }

    [Test]
    public void Step_RespectsDependencyOrder()
    {
        var executionOrder = new List<string>();
        var model1 = new TestSimulation("Model1", stepsToComplete: 5, onStep: () => executionOrder.Add("Model1"));
        var model2 = new TestSimulation("Model2", stepsToComplete: 5, onStep: () => executionOrder.Add("Model2"));

        // Model2 depends on Model1 (should execute after)
        _orchestrator.Register(model1);
        _orchestrator.Register(model2, new ModelOptions(), "Model1");
        _orchestrator.Initialize(_context);

        _orchestrator.Step();

        Assert.That(executionOrder, Is.EqualTo(new[] { "Model1", "Model2" }));
    }

    [Test]
    public void Step_CircularDependency_ThrowsOnInitialize()
    {
        var model1 = new TestSimulation("Model1", stepsToComplete: 5);
        var model2 = new TestSimulation("Model2", stepsToComplete: 5);

        _orchestrator.Register(model1, new ModelOptions(), "Model2");
        _orchestrator.Register(model2, new ModelOptions(), "Model1");

        Assert.Throws<OrchestratorException>(() => _orchestrator.Initialize(_context));
    }

    [Test]
    public void Step_RespectsPriority()
    {
        var executionOrder = new List<string>();
        var lowPriority = new TestSimulation("LowPriority", stepsToComplete: 5, onStep: () => executionOrder.Add("LowPriority"));
        var highPriority = new TestSimulation("HighPriority", stepsToComplete: 5, onStep: () => executionOrder.Add("HighPriority"));

        // Register low priority first, but high priority should execute first
        _orchestrator.Register(lowPriority, new ModelOptions { Priority = 200 });
        _orchestrator.Register(highPriority, new ModelOptions { Priority = 50 });
        _orchestrator.Initialize(_context);

        _orchestrator.Step();

        Assert.That(executionOrder, Is.EqualTo(new[] { "HighPriority", "LowPriority" }));
    }

    [Test]
    public void Step_OptionalModel_ContinuesOnError()
    {
        var goodModel = new TestSimulation("GoodModel", stepsToComplete: 5);
        var badModel = new TestSimulation("BadModel", stepsToComplete: 5, throwOnStep: true);

        _orchestrator.Register(goodModel);
        _orchestrator.Register(badModel, new ModelOptions { Optional = true });
        _orchestrator.Initialize(_context);

        // Should not throw
        var result = _orchestrator.Step();

        Assert.That(result, Is.EqualTo(OrchestratorStepResult.Continue));
        Assert.That(goodModel.StepsCalled, Is.EqualTo(1));
    }

    [Test]
    public void Step_RequiredModel_ThrowsOnError()
    {
        var badModel = new TestSimulation("BadModel", stepsToComplete: 5, throwOnStep: true);
        _orchestrator.Register(badModel);
        _orchestrator.Initialize(_context);

        Assert.Throws<OrchestratorException>(() => _orchestrator.Step());
        Assert.That(_orchestrator.State, Is.EqualTo(OrchestratorState.Error));
    }

    [Test]
    public void BeforeModelStep_EventRaised()
    {
        var model = new TestSimulation("Model1", stepsToComplete: 5);
        _orchestrator.Register(model);
        _orchestrator.Initialize(_context);

        var eventRaised = false;
        _orchestrator.BeforeModelStep += (sender, args) =>
        {
            eventRaised = true;
            Assert.That(args.Model.Id, Is.EqualTo("Model1"));
            Assert.That(args.StepNumber, Is.EqualTo(1));
        };

        _orchestrator.Step();

        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public void AfterModelStep_EventRaised()
    {
        var model = new TestSimulation("Model1", stepsToComplete: 5);
        _orchestrator.Register(model);
        _orchestrator.Initialize(_context);

        var eventRaised = false;
        _orchestrator.AfterModelStep += (sender, args) =>
        {
            eventRaised = true;
            Assert.That(args.Result, Is.EqualTo(SimulationStepResult.Continue));
        };

        _orchestrator.Step();

        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public void BarrierReached_EventRaisedAfterAllModels()
    {
        var model1 = new TestSimulation("Model1", stepsToComplete: 5);
        var model2 = new TestSimulation("Model2", stepsToComplete: 5);
        _orchestrator.Register(model1);
        _orchestrator.Register(model2);
        _orchestrator.Initialize(_context);

        var eventRaised = false;
        _orchestrator.BarrierReached += (sender, args) =>
        {
            eventRaised = true;
            Assert.That(args.StepNumber, Is.EqualTo(1));
            Assert.That(args.Models.Count, Is.EqualTo(2));
        };

        _orchestrator.Step();

        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public void SharedContext_ModelsCanCommunicate()
    {
        var writer = new TestSimulation("Writer", stepsToComplete: 5, onStep: () =>
        {
            // This will be called from the orchestrator context
        });
        var reader = new TestSimulation("Reader", stepsToComplete: 5);

        _orchestrator.Register(writer);
        _orchestrator.Register(reader, new ModelOptions(), "Writer");
        _orchestrator.Initialize(_context);

        // Set value in shared context
        _orchestrator.SharedContext.Set("weather", "sunny");

        // Models can read it
        Assert.That(_orchestrator.SharedContext.Get<string>("weather"), Is.EqualTo("sunny"));
    }

    /// <summary>
    /// Simple test simulation for orchestrator tests.
    /// </summary>
    private sealed class TestSimulation : ISimulation
    {
        private readonly int _stepsToComplete;
        private readonly bool _throwOnStep;
        private readonly Action? _onStep;
        private int _currentStep;

        public string Name { get; }
        public string Version => "1.0.0";
        public bool IsComplete => _currentStep >= _stepsToComplete;
        public bool WasInitialized { get; private set; }
        public int StepsCalled => _currentStep;

        public TestSimulation(string name, int stepsToComplete, bool throwOnStep = false, Action? onStep = null)
        {
            Name = name;
            _stepsToComplete = stepsToComplete;
            _throwOnStep = throwOnStep;
            _onStep = onStep;
        }

        public void Initialize(ISimulationContext context)
        {
            WasInitialized = true;
        }

        public SimulationStepResult Step()
        {
            if (_throwOnStep)
                throw new InvalidOperationException("Simulated error");

            _onStep?.Invoke();
            _currentStep++;

            return IsComplete ? SimulationStepResult.Completed : SimulationStepResult.Continue;
        }

        public void Dispose()
        {
            // Nothing to dispose, but required by ISimulation
        }
    }
}
