# DiamondX: A Baseball Game Simulator

DiamondX is a simple, text-based baseball game simulator written in C#. It uses player statistics to simulate at-bats and play out a full game.

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

## Project Structure

The project is organized into two main parts:

*   **`diamondx/`**: The main application.
    *   **`Core/`**: Contains the core game logic.
        *   **`Models/`**: Contains the `Player` and `AtBatOutcome` models.
    *   **`Program.cs`**: The application's entry point.
*   **`DiamondX.Tests/`**: Unit tests for the application.

## Running Tests

To run the unit tests, navigate to the root directory of the project and run:

```bash
dotnet test
```