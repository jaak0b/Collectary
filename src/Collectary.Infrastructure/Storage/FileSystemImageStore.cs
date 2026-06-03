using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Storage;

public class FileSystemImageStore : IImageStore
{
    private readonly string _basePath;

    public FileSystemImageStore(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream imageStream, string fileName)
    {
        var key = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_basePath, key);
        await using var file = File.Create(fullPath);
        await imageStream.CopyToAsync(file);
        return key;
    }

    public Stream Open(string imageKey) =>
        File.OpenRead(Path.Combine(_basePath, imageKey));

    public async Task DeleteAsync(string imageKey)
    {
        var fullPath = Path.Combine(_basePath, imageKey);
        if (File.Exists(fullPath))
            await Task.Run(() => File.Delete(fullPath));
    }

    public bool Exists(string imageKey) =>
        File.Exists(Path.Combine(_basePath, imageKey));
}
