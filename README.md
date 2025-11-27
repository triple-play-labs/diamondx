# DiamondX: A Baseball Game Simulator

DiamondX is an event-driven baseball game simulator written in C#. It uses player statistics to simulate at-bats and play out a full game, with a flexible event scheduler that can be extended to other simulation types.

## Getting Started

To get started, you'll need the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/diamondx.git
    cd diamondx
    ```
2.  **Build the project:**
    ```bash
    dotnet build
    ```
3.  **Run the game:**
    ```bash
    dotnet run
    ```

## Execution Options

### Normal Mode

Run a simulated game with human-readable console output:

```bash
dotnet run
```

### Debug Mode

Run with event scheduler debug output to see all events flowing through the system:

```bash
dotnet run -- --debug
# or
dotnet run -- -d
```

Debug mode shows:

- Event sequence numbers
- Timestamps
- Event types (e.g., `baseball.atbat.completed`)
- Handler dispatch information

## Project Structure

The project is organized into two main parts:

- **`diamondx/`**: The main application.
  - **`Core/`**: Contains the core game logic.
    - **`Events/`**: Event-driven architecture components.
      - **`ISimulationEvent.cs`**: Base event interface and `SimulationEventBase` record.
      - **`IEventHandler.cs`**: Handler interface for processing events.
      - **`EventScheduler.cs`**: Central event queue with sequencing, timestamps, and handler dispatch.
      - **`Baseball/`**: Baseball-specific event types (GameStarted, AtBat, RunScored, etc.).
      - **`Handlers/`**: Event handlers including `ConsoleEventHandler`.
    - **`Models/`**: Domain models (`Player`, `Pitcher`, `AtBatOutcome`).
    - **`State/`**: Game state management (`GameState`, `HalfInning`).
    - **`Simulation/`**: Simulation logic (`PlateAppearanceResolver` with log5 matchups).
    - **`Random/`**: Randomness abstraction (`IRandomSource`).
    - **`Game.cs`**: Main game orchestration.
  - **`Program.cs`**: The application's entry point.
- **`DiamondX.Tests/`**: Unit tests for the application.

## Architecture

DiamondX uses an event-driven architecture where all game actions emit events through a central `EventScheduler`. This design:

- **Decouples game logic from output**: The `ConsoleEventHandler` formats events for display, but you could add JSON, network, or database handlers.
- **Enables replay and analysis**: All events are logged with sequence numbers and timestamps.
- **Supports extensibility**: The `ISimulationEvent` interface is domain-agnostic, allowing the scheduler to be reused for other simulation types.

## Running Tests

To run the unit tests:

```bash
dotnet test
```

Or with verbose output:

```bash
dotnet test --verbosity normal
```
