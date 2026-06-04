namespace Collectary.Infrastructure.Persistence;

public class UserCredentialRecord
{
    public Guid UserId { get; set; }
    public byte[] Hash { get; set; } = Array.Empty<byte>();
    public byte[] Salt { get; set; } = Array.Empty<byte>();
    public int Iterations { get; set; }
    public string Algorithm { get; set; } = string.Empty;
}
