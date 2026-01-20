# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Install MAUI workload (required first time)
dotnet workload install maui-desktop

# Build entire solution
dotnet build Nouz/Nouz.slnx

# Build main app (Release)
dotnet build Nouz/src/Nouz/Nouz.csproj -c Release -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifierOverride=win10-x64

# Run all tests
dotnet test Nouz/Nouz.slnx

# Run specific test project
dotnet test Nouz/test/Nouz.Application.Unit.Tests
dotnet test Nouz/test/Nouz.Infrastructure.Unit.Tests
dotnet test Nouz/test/Nouz.Architecture.Tests

# Run single test by fully qualified name
dotnet test Nouz/Nouz.slnx --filter "FullyQualifiedName~NotebookHandlerTests.CreateNotebook"
```

## Architecture

This is a .NET 10 MAUI application with Blazor UI (WebView) for a note-taking app with notebooks, notes, and block-based editing.

### Layer Structure

```
Nouz (Main MAUI App)           - Blazor UI components in Components/
├── Nouz.Application           - Commands, handlers, actions, reducers, state
├── Nouz.Domain                - Entities, repository interfaces
├── Nouz.Infrastructure        - EF Core/SQLite repos, logging, preferences
└── Nouz.ReduxSimple           - Custom Redux state management library
```

**Layer dependency rules (enforced by architecture tests):**
- Domain has no dependencies on other layers
- Application depends only on Domain
- Infrastructure implements Domain interfaces but is not referenced by Application
- Only `ServiceCollectionExtensions` classes are public in Infrastructure

### State Management Pattern

Redux-like pattern with immutable state:
1. UI dispatches **Commands** via `IMediator` (e.g., `NotebookCommands.CreateNotebook`)
2. **Handlers** process commands, call repositories, dispatch **Actions**
3. **Reducers** produce new state from actions
4. UI subscribes to state via selectors

Key interfaces:
- `IActionDispatcher` - dispatches actions to the Redux store
- `IStateProvider` - provides access to current state
- `ICommandHandler<T>` - handles commands (from Mediator library)

State is defined in `RootState.cs` combining `NotebookState`, `NoteState`, `NotificationState`.

### CQRS Commands Pattern

Commands are defined as sealed records in `*Commands.cs` files:
```csharp
public static class NotebookCommands
{
    public sealed record CreateNotebook(string Title) : ICommand;
}
```

Handlers implement `ICommandHandler<T>` and are registered via source generation.

### Key Technologies

- **UI**: Blazor components with Radzen.Blazor, rendered in MAUI WebView
- **State**: Custom Redux (`Nouz.ReduxSimple`) with System.Reactive
- **CQRS**: Mediator library with source generation
- **Database**: EF Core with SQLite
- **Logging**: Serilog (configured via appsettings.json embedded resource)
- **Testing**: xUnit, NSubstitute, Shouldly, NetArchTest

### Project Configuration

- Package versions centralized in `Nouz/Directory.Packages.props`
- `TreatWarningsAsErrors=true` in all projects
- Nullable reference types enabled
- DI registration in `MauiProgram.cs` via extension methods from each layer

### Frontend/backend separation
CSS code shall reside in a separate `.razor.css` file and similarly the c# code shall reside in a `.razor.cs` file and not be mixed with the frontend code.

### Workflow
When implementing new features or refactoring existing features, please follow the following workflow:

- Create a detailed plan and outline steps
- Implement step-by-step, after each step, add relevant unit and integration tests and run them to make sure everything still works
- Verify implementation at the end by running all tests

## Design
The design is kept minimalistic and almost monochrome with a warm tone. For every new feature, stick to the elegant, minimalistic design and re-use the 
color palette already found in the app.