using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class CredentialStore : ICredentialStore
{
    private readonly Func<InventoryDbContext> _dbFactory;

    public CredentialStore(Func<InventoryDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task SaveAsync(Guid userId, PasswordHash credential)
    {
        using var db = _dbFactory();
        var existing = await db.UserCredentials.FirstOrDefaultAsync(c => c.UserId == userId);
        if (existing is null)
        {
            db.UserCredentials.Add(new UserCredentialRecord
            {
                UserId = userId,
                Hash = credential.Hash,
                Salt = credential.Salt,
                Iterations = credential.Iterations,
                Algorithm = credential.Algorithm,
            });
        }
        else
        {
            existing.Hash = credential.Hash;
            existing.Salt = credential.Salt;
            existing.Iterations = credential.Iterations;
            existing.Algorithm = credential.Algorithm;
        }

        await db.SaveChangesAsync();
    }

    public async Task<PasswordHash?> GetAsync(Guid userId)
    {
        using var db = _dbFactory();
        var record = await db.UserCredentials.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
        return record is null
            ? null
            : new PasswordHash(record.Hash, record.Salt, record.Iterations, record.Algorithm);
    }
}
