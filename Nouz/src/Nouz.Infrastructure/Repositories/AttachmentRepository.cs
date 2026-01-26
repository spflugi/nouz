using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Repositories;

internal sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly string _attachmentsDirectory;

    public AttachmentRepository()
    {
#if DEBUG
        _attachmentsDirectory = Path.Combine(FileSystem.AppDataDirectory, "dev", "attachments");
#else
        _attachmentsDirectory = Path.Combine(FileSystem.AppDataDirectory, "attachments");
#endif

        Directory.CreateDirectory(_attachmentsDirectory);
    }

    public async Task SaveAsync(Guid id, byte[] data, string extension, CancellationToken token = default)
    {
        var fileName = GetFileName(id, extension);
        var filePath = Path.Combine(_attachmentsDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, data, token).ConfigureAwait(false);
    }

    public async Task<byte[]?> LoadAsync(Guid id, CancellationToken token = default)
    {
        var filePath = await GetPathAsync(id, token).ConfigureAwait(false);
        if (filePath is null)
        {
            return null;
        }

        return await File.ReadAllBytesAsync(filePath, token).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        var files = Directory.GetFiles(_attachmentsDirectory, $"{id}.*");
        foreach (var file in files)
        {
            File.Delete(file);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken token = default)
    {
        var files = Directory.GetFiles(_attachmentsDirectory, $"{id}.*");
        return Task.FromResult(files.Length > 0);
    }

    public Task<string?> GetPathAsync(Guid id, CancellationToken token = default)
    {
        var files = Directory.GetFiles(_attachmentsDirectory, $"{id}.*");
        return Task.FromResult(files.Length > 0 ? files[0] : null);
    }

    private static string GetFileName(Guid id, string extension)
    {
        // Ensure extension starts with a dot
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return $"{id}{extension}";
    }
}
