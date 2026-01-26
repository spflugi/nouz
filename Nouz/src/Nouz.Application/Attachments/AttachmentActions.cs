using System.Collections.Immutable;
using Nouz.Application.Store;
using Nouz.Domain.Entities;

namespace Nouz.Application.Attachments;

public static class AttachmentActions
{
    /// <summary>
    /// Triggered when attachments for a note are loaded.
    /// </summary>
    public sealed record AttachmentsLoaded(Guid NoteId, ImmutableList<NoteAttachment> Attachments) : IAction;

    /// <summary>
    /// Triggered when an attachment is added to a note.
    /// </summary>
    public sealed record AttachmentAdded(Guid NoteId, NoteAttachment Attachment) : IAction;

    /// <summary>
    /// Triggered when an attachment is deleted from a note.
    /// </summary>
    public sealed record AttachmentDeleted(Guid NoteId, Guid AttachmentId) : IAction;

    /// <summary>
    /// Triggered when all attachments for a note are cleared (e.g., when note is deleted).
    /// </summary>
    public sealed record AttachmentsCleared(Guid NoteId) : IAction;
}
