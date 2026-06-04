using System.Security.Cryptography;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Security;

public class Pbkdf2CredentialHasher : ICredentialHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 64;
    private const int DefaultIterations = 210_000;
    private const string AlgorithmName = "PBKDF2-HMAC-SHA512";

    public PasswordHash Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations, HashAlgorithmName.SHA512, KeySize);
        return new PasswordHash(hash, salt, DefaultIterations, AlgorithmName);
    }

    public bool Verify(string password, PasswordHash stored)
    {
        var algorithm = ResolveAlgorithm(stored.Algorithm);
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, stored.Salt, stored.Iterations, algorithm, stored.Hash.Length);
        return CryptographicOperations.FixedTimeEquals(computed, stored.Hash);
    }

    private HashAlgorithmName ResolveAlgorithm(string algorithm) => algorithm switch
    {
        AlgorithmName => HashAlgorithmName.SHA512,
        _ => throw new NotSupportedException($"Unsupported password hash algorithm '{algorithm}'."),
    };
}
