using Mediator;
using Nouz.Application.Embeddings;
using Nouz.Application.Logger;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Nouz.Application.Unit.Tests.Notes;

public class NoteHandlerMoveNoteTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly INoteRepository _noteRepository = Substitute.For<INoteRepository>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IEmbeddingRepository _embeddingRepository = Substitute.For<IEmbeddingRepository>();
    private readonly IAttachmentRepository _attachmentRepository = Substitute.For<IAttachmentRepository>();
    private readonly INoteAttachmentRepository _noteAttachmentRepository = Substitute.For<INoteAttachmentRepository>();
    private readonly IStateProvider _stateProvider = Substitute.For<IStateProvider>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<NoteHandler> _logger = Substitute.For<ILoggerAdapter<NoteHandler>>();
    private readonly NoteExportService _noteExportService;
    private readonly NoteHandler _handler;

    public NoteHandlerMoveNoteTests()
    {
        _noteExportService = new NoteExportService(_attachmentRepository);
        _handler = new NoteHandler(
            _mediator,
            _noteRepository,
            _embeddingService,
            _embeddingRepository,
            _attachmentRepository,
            _noteAttachmentRepository,
            _stateProvider,
            _actionDispatcher,
            _logger,
            _noteExportService);
    }

    [Fact]
    public async Task MoveNote_ShouldMoveNoteAndDispatchAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var newNotebookId = Guid.NewGuid();

        // Act
        await _handler.Handle(new NoteCommands.MoveNote(noteId, newNotebookId), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).MoveToNotebook(noteId, newNotebookId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteMoved>(a => a.NoteId == noteId && a.NewNotebookId == newNotebookId));
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Moved" &&
                n.Message == "Note moved to notebook." &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveNote_ShouldLogMoveOperation()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var newNotebookId = Guid.NewGuid();

        // Act
        await _handler.Handle(new NoteCommands.MoveNote(noteId, newNotebookId), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogInformation("Moving note '{NoteId}' to notebook '{NewNotebookId}'", noteId, newNotebookId);
        _logger.Received(1).LogInformation("Note '{NoteId}' moved to notebook '{NewNotebookId}'", noteId, newNotebookId);
    }

    [Fact]
    public async Task MoveNote_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var newNotebookId = Guid.NewGuid();
        var exception = new Exception("Database error");
        _noteRepository.MoveToNotebook(noteId, newNotebookId, Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NoteCommands.MoveNote(noteId, newNotebookId), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to move note '{NoteId}' to notebook '{NewNotebookId}'", noteId, newNotebookId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Failed to move note." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteMoved>());
    }
}
