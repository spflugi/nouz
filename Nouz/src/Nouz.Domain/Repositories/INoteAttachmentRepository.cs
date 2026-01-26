using Nouz.Domain.Entities;

namespace Nouz.Domain.Repositories;

public interface INoteAttachmentRepository
{
    /// <summary>
    /// Add a new attachment to a note.
    /// </summary>
    /// <param name="noteId">The note to attach the file to</param>
    /// <param name="fileStream">Stream containing the file data</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>The created attachment, or an existing one if duplicate detected</returns>
    Task<NoteAttachment> AddAsync(Guid noteId, Stream fileStream, string fileName, CancellationToken token = default);

    /// <summary>
    /// Get all attachments for a note.
    /// </summary>
    Task<IReadOnlyList<NoteAttachment>> GetByNoteIdAsync(Guid noteId, CancellationToken token = default);

    /// <summary>
    /// Get the file path for an attachment (for opening with default app).
    /// </summary>
    Task<string?> GetFilePathAsync(Guid attachmentId, CancellationToken token = default);

    /// <summary>
    /// Delete an attachment.
    /// </summary>
    Task DeleteAsync(Guid attachmentId, CancellationToken token = default);

    /// <summary>
    /// Delete all attachments for a note.
    /// </summary>
    Task DeleteAllForNoteAsync(Guid noteId, CancellationToken token = default);

    /// <summary>
    /// Find an attachment by content hash within a note (for duplicate detection).
    /// </summary>
    Task<NoteAttachment?> FindByHashAsync(Guid noteId, string contentHash, CancellationToken token = default);
}
