using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notebooks;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Lifecycle;

internal sealed class LifecycleHandler :
    ICommandHandler<LifecycleCommands.PerformOnAppStart>,
    ICommandHandler<LifecycleCommands.PerformOnAppResume>,
    ICommandHandler<LifecycleCommands.PerformOnAppSleep>
{
    private readonly IMediator _mediator;
    private readonly IDbMigrator _dbMigrator;
    private readonly ILoggerAdapter<LifecycleHandler> _logger;

    public LifecycleHandler(IMediator mediator, IDbMigrator dbMigrator, ILoggerAdapter<LifecycleHandler> logger)
    {
        _mediator = mediator;
        _dbMigrator = dbMigrator;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppStart command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize app lifecycle on start.");

        await _dbMigrator.ApplyMigrations(cancellationToken).ConfigureAwait(false);
        await _mediator.Send(new NotebookCommands.LoadAllNotebooks(), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
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