namespace Collectary.Core.Ports;

public record PasswordHash(byte[] Hash, byte[] Salt, int Iterations, string Algorithm);
