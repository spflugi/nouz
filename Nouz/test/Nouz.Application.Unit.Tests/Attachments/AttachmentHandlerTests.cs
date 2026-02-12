using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Attachments;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Attachments;

public class AttachmentHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly INoteAttachmentRepository _attachmentRepository = Substitute.For<INoteAttachmentRepository>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<AttachmentHandler> _logger = Substitute.For<ILoggerAdapter<AttachmentHandler>>();
    private readonly AttachmentHandler _handler;

    public AttachmentHandlerTests()
    {
        _handler = new AttachmentHandler(_mediator, _attachmentRepository, _actionDispatcher, _logger);
    }

    #region LoadAttachments Tests

    [Fact]
    public async Task LoadAttachments_ShouldLoadAndDispatchAttachments()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachments = new List<NoteAttachment>
        {
            CreateAttachment(noteId, "file1.pdf"),
            CreateAttachment(noteId, "file2.pdf")
        };
        _attachmentRepository.GetByNoteIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(attachments);

        // Act
        await _handler.Handle(new AttachmentCommands.LoadAttachments(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).GetByNoteIdAsync(noteId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<AttachmentActions.AttachmentsLoaded>(a =>
                a.NoteId == noteId &&
                a.Attachments.Count == 2));
    }

    [Fact]
    public async Task LoadAttachments_WhenNoAttachments_ShouldDispatchEmptyList()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _attachmentRepository.GetByNoteIdAsync(noteId, Arg.Any<CancellationToken>())
            .Returns(new List<NoteAttachment>());

        // Act
        await _handler.Handle(new AttachmentCommands.LoadAttachments(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<AttachmentActions.AttachmentsLoaded>(a => a.Attachments.Count == 0));
    }

    [Fact]
    public async Task LoadAttachments_WhenRepositoryThrows_ShouldLogError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var exception = new Exception("Database error");
        _attachmentRepository.GetByNoteIdAsync(noteId, Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new AttachmentCommands.LoadAttachments(noteId), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to load attachments for note '{NoteId}'", noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<AttachmentActions.AttachmentsLoaded>());
    }

    #endregion

    #region AddAttachment Tests

    [Fact]
    public async Task AddAttachment_ShouldAddAndDispatchAttachment()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var fileName = "test.pdf";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var attachment = CreateAttachment(noteId, fileName);
        _attachmentRepository.AddAsync(noteId, stream, fileName, Arg.Any<CancellationToken>())
            .Returns(attachment);

        // Act
        await _handler.Handle(new AttachmentCommands.AddAttachment(noteId, stream, fileName), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).AddAsync(noteId, stream, fileName, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<AttachmentActions.AttachmentAdded>(a =>
                a.NoteId == noteId &&
                a.Attachment.FileName == fileName));
    }

    [Fact]
    public async Task AddAttachment_ShouldShowSuccessNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var fileName = "document.pdf";
        using var stream = new MemoryStream();
        var attachment = CreateAttachment(noteId, fileName);
        _attachmentRepository.AddAsync(noteId, stream, fileName, Arg.Any<CancellationToken>())
            .Returns(attachment);

        // Act
        await _handler.Handle(new AttachmentCommands.AddAttachment(noteId, stream, fileName), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Attached" &&
                n.Message.Contains(fileName) &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAttachment_WhenFileTooLarge_ShouldShowSizeError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var fileName = "large-file.pdf";
        using var stream = new MemoryStream();
        _attachmentRepository.AddAsync(noteId, stream, fileName, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("File exceeds maximum size limit"));

        // Act
        await _handler.Handle(new AttachmentCommands.AddAttachment(noteId, stream, fileName), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message.Contains("50MB") &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAttachment_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var fileName = "file.pdf";
        using var stream = new MemoryStream();
        var exception = new Exception("Storage error");
        _attachmentRepository.AddAsync(noteId, stream, fileName, Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(new AttachmentCommands.AddAttachment(noteId, stream, fileName), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to add attachment '{FileName}' to note '{NoteId}'",
            fileName, noteId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteAttachment Tests

    [Fact]
    public async Task DeleteAttachment_ShouldDeleteAndDispatch()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();

        // Act
        await _handler.Handle(new AttachmentCommands.DeleteAttachment(noteId, attachmentId), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<AttachmentActions.AttachmentDeleted>(a =>
                a.NoteId == noteId &&
                a.AttachmentId == attachmentId));
    }

    [Fact]
    public async Task DeleteAttachment_ShouldShowSuccessNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();

        // Act
        await _handler.Handle(new AttachmentCommands.DeleteAttachment(noteId, attachmentId), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Deleted" &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAttachment_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var exception = new Exception("Delete error");
        _attachmentRepository.DeleteAsync(attachmentId, Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new AttachmentCommands.DeleteAttachment(noteId, attachmentId), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to delete attachment '{AttachmentId}' from note '{NoteId}'",
            attachmentId, noteId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region OpenAttachment Tests

    [Fact]
    public async Task OpenAttachment_WhenFileNotFound_ShouldShowErrorNotification()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        _attachmentRepository.GetFilePathAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        await _handler.Handle(new AttachmentCommands.OpenAttachment(attachmentId), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogWarning("Attachment '{AttachmentId}' file not found", attachmentId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message.Contains("not found") &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenAttachment_WhenFilePathEmpty_ShouldShowErrorNotification()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        _attachmentRepository.GetFilePathAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        // Act
        await _handler.Handle(new AttachmentCommands.OpenAttachment(attachmentId), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message.Contains("not found")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenAttachment_WhenProcessStartThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        // Return a valid path but Process.Start will fail since it's a test environment
        _attachmentRepository.GetFilePathAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns("/non/existent/path/file.pdf");

        // Act
        await _handler.Handle(new AttachmentCommands.OpenAttachment(attachmentId), TestContext.Current.CancellationToken);

        // Assert - either it works or throws, both handled gracefully
        // The handler catches exceptions and shows error notification
        await _mediator.Received().Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    private static NoteAttachment CreateAttachment(Guid noteId, string fileName)
    {
        return new NoteAttachment
        {
            Id = Guid.NewGuid(),
            NoteId = noteId,
            FileName = fileName,
            Extension = ".pdf",
            FileSizeBytes = 1024,
            ContentHash = "abc123",
            AddedAt = DateTimeOffset.UtcNow
        };
    }
}
