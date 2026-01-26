using System.Collections.Immutable;
using System.Diagnostics;
using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Attachments;

internal sealed class AttachmentHandler :
    ICommandHandler<AttachmentCommands.LoadAttachments>,
    ICommandHandler<AttachmentCommands.AddAttachment>,
    ICommandHandler<AttachmentCommands.DeleteAttachment>,
    ICommandHandler<AttachmentCommands.OpenAttachment>
{
    private readonly IMediator _mediator;
    private readonly INoteAttachmentRepository _attachmentRepository;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<AttachmentHandler> _logger;

    public AttachmentHandler(
        IMediator mediator,
        INoteAttachmentRepository attachmentRepository,
        IActionDispatcher actionDispatcher,
        ILoggerAdapter<AttachmentHandler> logger)
    {
        _mediator = mediator;
        _attachmentRepository = attachmentRepository;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(AttachmentCommands.LoadAttachments command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading attachments for note '{NoteId}'", command.NoteId);

        try
        {
            var attachments = await _attachmentRepository.GetByNoteIdAsync(command.NoteId, cancellationToken)
                .ConfigureAwait(false);

            await _actionDispatcher.Dispatch(new AttachmentActions.AttachmentsLoaded(
                command.NoteId,
                attachments.ToImmutableList())).ConfigureAwait(false);

            _logger.LogDebug("{Count} attachments loaded for note '{NoteId}'", attachments.Count, command.NoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attachments for note '{NoteId}'", command.NoteId);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(AttachmentCommands.AddAttachment command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding attachment '{FileName}' to note '{NoteId}'", command.FileName, command.NoteId);

        try
        {
            var attachment = await _attachmentRepository.AddAsync(
                command.NoteId,
                command.FileStream,
                command.FileName,
                cancellationToken).ConfigureAwait(false);

            await _actionDispatcher.Dispatch(new AttachmentActions.AttachmentAdded(command.NoteId, attachment))
                .ConfigureAwait(false);

            _logger.LogInformation("Attachment '{FileName}' added to note '{NoteId}' with ID '{AttachmentId}'",
                command.FileName, command.NoteId, attachment.Id);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Attached",
                $"File '{command.FileName}' attached.",
                NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exceeds maximum size"))
        {
            _logger.LogWarning(ex, "Attachment '{FileName}' exceeds maximum file size", command.FileName);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Error",
                "File exceeds 50MB limit.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add attachment '{FileName}' to note '{NoteId}'",
                command.FileName, command.NoteId);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Error",
                "Failed to add attachment.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(AttachmentCommands.DeleteAttachment command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting attachment '{AttachmentId}' from note '{NoteId}'",
            command.AttachmentId, command.NoteId);

        try
        {
            await _attachmentRepository.DeleteAsync(command.AttachmentId, cancellationToken).ConfigureAwait(false);

            await _actionDispatcher.Dispatch(new AttachmentActions.AttachmentDeleted(
                command.NoteId,
                command.AttachmentId)).ConfigureAwait(false);

            _logger.LogInformation("Attachment '{AttachmentId}' deleted from note '{NoteId}'",
                command.AttachmentId, command.NoteId);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Deleted",
                "Attachment removed.",
                NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete attachment '{AttachmentId}' from note '{NoteId}'",
                command.AttachmentId, command.NoteId);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Error",
                "Failed to delete attachment.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(AttachmentCommands.OpenAttachment command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening attachment '{AttachmentId}'", command.AttachmentId);

        try
        {
            var filePath = await _attachmentRepository.GetFilePathAsync(command.AttachmentId, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("Attachment '{AttachmentId}' file not found", command.AttachmentId);

                await _mediator.Send(new NotificationCommands.ShowNotification(
                    "Error",
                    "Attachment file not found.",
                    NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);

                return Unit.Value;
            }

            // Open the file with the default application
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            _logger.LogDebug("Opened attachment '{AttachmentId}' at path '{FilePath}'", command.AttachmentId, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open attachment '{AttachmentId}'", command.AttachmentId);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Error",
                "Failed to open attachment.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
