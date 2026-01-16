using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Notebooks;

internal sealed class NotebookHandler :
    ICommandHandler<NotebookCommands.LoadAllNotebooks>,
    ICommandHandler<NotebookCommands.CreateNotebook>,
    ICommandHandler<NotebookCommands.SelectNotebook>,
    ICommandHandler<NotebookCommands.RenameNotebook>,
    ICommandHandler<NotebookCommands.DeleteNotebook>
{
    private readonly IMediator _mediator;
    private readonly INotebookRepository _notebookRepository;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<NotebookHandler> _logger;

    public NotebookHandler(IMediator mediator, INotebookRepository notebookRepository,
        IActionDispatcher actionDispatcher,
        ILoggerAdapter<NotebookHandler> logger)
    {
        _mediator = mediator;
        _notebookRepository = notebookRepository;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(NotebookCommands.LoadAllNotebooks command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading all available notebooks");

        try
        {
            var allNotebooks = await _notebookRepository.GetAll(cancellationToken).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(
                new NotebookActions.AllNotebooksLoaded(ImmutableList.CreateRange(allNotebooks))).ConfigureAwait(false);

            _logger.LogInformation("{NumNotebooks} successfully loaded", allNotebooks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load all notebooks");

            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Failed to load all notebooks.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NotebookCommands.CreateNotebook command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new notebook with the title '{NotebookTitle}'", command.Title);

        try
        {
            var now = DateTimeOffset.UtcNow;

            var notebook = new Notebook
            {
                Id = Guid.NewGuid(),
                Name = command.Title,
                CreatedAt = now,
                LastModifiedAt = now,
                SortOrder = 1
            };

            await _notebookRepository.Add(notebook, cancellationToken).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(new NotebookActions.NotebookAdded(notebook)).ConfigureAwait(false);

            _logger.LogInformation("New notebook with title '{NotebookTitle}' successfully created", command.Title);
            
            await _mediator.Send(new NotificationCommands.ShowNotification("Success", "New notebook successfully created.",
                NotificationSeverity.Info), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new notebook");

            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Failed to create new notebook.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NotebookCommands.SelectNotebook command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Selecting the notebook with the id '{NotebookId}'", command.Id);

        await _actionDispatcher.Dispatch(new NotebookActions.NotebookSelected(command.Id)).ConfigureAwait(false);

        _logger.LogInformation("New notebook with id '{NotebookId}' selected", command.Id);

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NotebookCommands.RenameNotebook command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Renaming notebook with id '{NotebookId}' to '{NewTitle}'", command.Id, command.NewTitle);

        var existingNotebook = await _notebookRepository.GetById(command.Id, cancellationToken).ConfigureAwait(false);
        
        if (existingNotebook is null)
        {
            _logger.LogWarning("Cannot rename notebook. Notebook with id '{NotebookId}' not found", command.Id);
            
            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Notebook not found.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        }

        var renamedNotebook = existingNotebook with
        {
            Name = command.NewTitle,
            LastModifiedAt = DateTimeOffset.UtcNow
        };

        await _notebookRepository.Update(renamedNotebook, cancellationToken).ConfigureAwait(false);
        await _actionDispatcher.Dispatch(new NotebookActions.NotebookUpdated(renamedNotebook)).ConfigureAwait(false);

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NotebookCommands.DeleteNotebook command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting notebook with id '{NotebookId}'", command.Id);

        await _notebookRepository.Delete(command.Id);

        _logger.LogInformation("Notebook with id '{NotebookId}' deleted", command.Id);

        await _mediator.Send(new NotificationCommands.ShowNotification("Deleted", "Notebook deleted",
            NotificationSeverity.Info), cancellationToken).ConfigureAwait(false);

        await _actionDispatcher.Dispatch(new NotebookActions.NotebookDeleted(command.Id));
        return Unit.Value;
    }
}