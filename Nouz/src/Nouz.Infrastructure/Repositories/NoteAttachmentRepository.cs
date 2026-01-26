using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Repositories;

internal sealed class NoteAttachmentRepository : INoteAttachmentRepository
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private readonly string _baseDirectory;
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;

    public NoteAttachmentRepository(IDbContextFactory<NouzDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _baseDirectory = Path.Combine(FileSystem.AppDataDirectory, "note-attachments");
        Directory.CreateDirectory(_baseDirectory);
    }

    public async Task<NoteAttachment> AddAsync(Guid noteId, Stream fileStream, string fileName, CancellationToken token = default)
    {
        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024}MB");
        }

        // Read the stream into memory for hash computation
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, token).ConfigureAwait(false);
        var data = memoryStream.ToArray();

        // Compute hash for duplicate detection
        var contentHash = ComputeSha256Hash(data);

        // Check for existing attachment with same hash in this note
        var existing = await FindByHashAsync(noteId, contentHash, token).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        // Create the attachment record
        var attachmentId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var attachment = new NoteAttachment
        {
            Id = attachmentId,
            NoteId = noteId,
            FileName = fileName,
            Extension = extension,
            FileSizeBytes = data.Length,
            ContentHash = contentHash,
            AddedAt = DateTimeOffset.UtcNow
        };

        // Ensure note directory exists
        var noteDirectory = GetNoteDirectory(noteId);
        Directory.CreateDirectory(noteDirectory);

        // Save file to disk
        var filePath = GetStoragePath(noteId, attachmentId, fileName);
        await File.WriteAllBytesAsync(filePath, data, token).ConfigureAwait(false);

        // Save metadata to database
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
        context.NoteAttachments.Add(attachment);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return attachment;
    }

    public async Task<IReadOnlyList<NoteAttachment>> GetByNoteIdAsync(Guid noteId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        // SQLite doesn't support DateTimeOffset in ORDER BY, so we order client-side
        var attachments = await context.NoteAttachments
            .Where(a => a.NoteId == noteId)
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        return attachments.OrderByDescending(a => a.AddedAt).ToList();
    }

    public async Task<string?> GetFilePathAsync(Guid attachmentId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var attachment = await context.NoteAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, token)
            .ConfigureAwait(false);

        if (attachment is null)
        {
            return null;
        }

        var filePath = GetStoragePath(attachment.NoteId, attachment.Id, attachment.FileName);
        return File.Exists(filePath) ? filePath : null;
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var attachment = await context.NoteAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId, token)
            .ConfigureAwait(false);

        if (attachment is null)
        {
            return;
        }

        // Delete file from disk
        var filePath = GetStoragePath(attachment.NoteId, attachment.Id, attachment.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Delete from database
        context.NoteAttachments.Remove(attachment);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        // Clean up note directory if empty
        CleanupNoteDirectoryIfEmpty(attachment.NoteId);
    }

    public async Task DeleteAllForNoteAsync(Guid noteId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var attachments = await context.NoteAttachments
            .Where(a => a.NoteId == noteId)
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (attachments.Count == 0)
        {
            return;
        }

        // Delete files from disk
        foreach (var attachment in attachments)
        {
            var filePath = GetStoragePath(attachment.NoteId, attachment.Id, attachment.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        // Delete from database
        context.NoteAttachments.RemoveRange(attachments);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        // Clean up note directory
        CleanupNoteDirectoryIfEmpty(noteId);
    }

    public async Task<NoteAttachment?> FindByHashAsync(Guid noteId, string contentHash, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        return await context.NoteAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.NoteId == noteId && a.ContentHash == contentHash, token)
            .ConfigureAwait(false);
    }

    private string GetNoteDirectory(Guid noteId)
    {
        return Path.Combine(_baseDirectory, noteId.ToString());
    }

    private string GetStoragePath(Guid noteId, Guid attachmentId, string fileName)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        return Path.Combine(_baseDirectory, noteId.ToString(), $"{attachmentId}_{sanitizedFileName}");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c)));
    }

    private static string ComputeSha256Hash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes);
    }

    private void CleanupNoteDirectoryIfEmpty(Guid noteId)
    {
        var noteDirectory = GetNoteDirectory(noteId);
        if (Directory.Exists(noteDirectory) && !Directory.EnumerateFileSystemEntries(noteDirectory).Any())
        {
            Directory.Delete(noteDirectory);
        }
    }
}
