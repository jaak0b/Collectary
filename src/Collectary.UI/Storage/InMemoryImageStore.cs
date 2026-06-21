using System.Collections.Concurrent;
using Collectary.Core.Ports;

namespace Collectary.UI.Storage;

public class InMemoryImageStore : IImageStore
{
    private readonly ConcurrentDictionary<string, byte[]> _images = new();

    public async Task<string> SaveAsync(Stream imageStream, string fileName)
    {
        using var buffer = new MemoryStream();
        await imageStream.CopyToAsync(buffer);
        var key = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
        _images[key] = buffer.ToArray();
        return key;
    }

    public Stream Open(string imageKey) =>
        _images.TryGetValue(imageKey, out var data)
            ? new MemoryStream(data)
            : throw new FileNotFoundException($"Image not found: {imageKey}");

    public Task DeleteAsync(string imageKey)
    {
        _images.TryRemove(imageKey, out _);
        return Task.CompletedTask;
    }

    public bool Exists(string imageKey) => _images.ContainsKey(imageKey);

    public Task<IReadOnlyList<string>> ListKeysAsync() =>
        Task.FromResult<IReadOnlyList<string>>(_images.Keys.ToList());

    public async Task ImportAsync(string imageKey, Stream content)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        _images[imageKey] = buffer.ToArray();
    }
}
