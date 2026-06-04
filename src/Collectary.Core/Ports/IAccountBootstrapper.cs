using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IAccountBootstrapper
{
    Task<User> EnsureDefaultUserAsync();
    Task BackfillOwnerlessAsync(Guid ownerId);
}
