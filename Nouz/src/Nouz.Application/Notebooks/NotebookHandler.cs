using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Notebooks;

public class NotebookHandler :
    ICommandHandler<NotebookCommands.LoadAllNotebooks>,
    ICommandHandler<NotebookCommands.CreateNotebook>,
    ICommandHandler<NotebookCommands.SelectNotebook>
{
    private readonly INotebookRepository _notebookRepository;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<NotebookHandler> _logger;

    public NotebookHandler(INotebookRepository notebookRepository, IActionDispatcher actionDispatcher,
        ILoggerAdapter<NotebookHandler> logger)
    {
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

            // TODO: Show error notification to user
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new notebook");

            // TODO: Show error notification to user
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
}