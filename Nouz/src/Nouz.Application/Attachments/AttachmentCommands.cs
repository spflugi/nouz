using Mediator;

namespace Nouz.Application.Attachments;

public static class AttachmentCommands
{
    /// <summary>
    /// Load all attachments for a specific note.
    /// </summary>
    public sealed record LoadAttachments(Guid NoteId) : ICommand;

    /// <summary>
    /// Add an attachment to a note.
    /// </summary>
    /// <param name="NoteId">The note to attach the file to</param>
    /// <param name="FileStream">Stream containing the file data</param>
    /// <param name="FileName">Original file name</param>
    public sealed record AddAttachment(Guid NoteId, Stream FileStream, string FileName) : ICommand;

    /// <summary>
    /// Delete an attachment from a note.
    /// </summary>
    public sealed record DeleteAttachment(Guid NoteId, Guid AttachmentId) : ICommand;

    /// <summary>
    /// Open an attachment with the default application.
    /// </summary>
    public sealed record OpenAttachment(Guid AttachmentId) : ICommand;
}
