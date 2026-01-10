using Mediator;

namespace Nouz.Application.Lifecycle;

public static class LifecycleCommands
{
    /// <summary>
    /// Called when the app starts. Initializes the app.
    /// </summary>
    public sealed record PerformOnAppStart : ICommand;

    /// <summary>
    /// Called when the app resumes.
    /// </summary>
    public sealed record PerformOnAppResume : ICommand;

    /// <summary>
    /// Called when app goes to sleep.
    /// </summary>
    public sealed record PerformOnAppSleep : ICommand;
}