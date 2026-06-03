namespace Collectary.Core.Ports;

public interface IImageStore
{
    Task<string> SaveAsync(Stream imageStream, string fileName);
    Stream Open(string imageKey);
    Task DeleteAsync(string imageKey);
    bool Exists(string imageKey);
}
