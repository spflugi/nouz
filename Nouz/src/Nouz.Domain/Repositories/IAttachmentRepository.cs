namespace Nouz.Domain.Repositories;

public interface IAttachmentRepository
{
    /// <summary>
    /// Save an attachment to storage.
    /// </summary>
    /// <param name="id">Unique identifier for the attachment</param>
    /// <param name="data">Binary data of the attachment</param>
    /// <param name="extension">File extension (e.g., ".png", ".jpg")</param>
    Task SaveAsync(Guid id, byte[] data, string extension, CancellationToken token = default);

    /// <summary>
    /// Load an attachment from storage.
    /// </summary>
    /// <param name="id">Unique identifier for the attachment</param>
    /// <returns>Binary data of the attachment, or null if not found</returns>
    Task<byte[]?> LoadAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Delete an attachment from storage.
    /// </summary>
    /// <param name="id">Unique identifier for the attachment</param>
    Task DeleteAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Check if an attachment exists in storage.
    /// </summary>
    /// <param name="id">Unique identifier for the attachment</param>
    Task<bool> ExistsAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Get the file path for an attachment (for direct file access).
    /// </summary>
    /// <param name="id">Unique identifier for the attachment</param>
    /// <returns>Full file path, or null if not found</returns>
    Task<string?> GetPathAsync(Guid id, CancellationToken token = default);
}
