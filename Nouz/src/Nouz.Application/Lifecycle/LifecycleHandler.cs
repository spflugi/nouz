using Mediator;
using Nouz.Application.Logger;

namespace Nouz.Application.Lifecycle;

internal sealed class LifecycleHandler :
    ICommandHandler<LifecycleCommands.PerformOnAppStart>,
    ICommandHandler<LifecycleCommands.PerformOnAppResume>,
    ICommandHandler<LifecycleCommands.PerformOnAppSleep>
{
    private readonly ILoggerAdapter<LifecycleHandler> _logger;

    public LifecycleHandler(ILoggerAdapter<LifecycleHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppStart command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize app lifecycle on start.");
        return ValueTask.FromResult(Unit.Value);
    }

    public ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppResume command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize app lifecycle on resume.");
        return ValueTask.FromResult(Unit.Value);
    }

    public ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppSleep command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("App going to sleep.");
        return ValueTask.FromResult(Unit.Value);
    }
}