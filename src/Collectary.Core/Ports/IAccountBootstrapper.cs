namespace Collectary.Core.Ports;

public interface IAccountBootstrapper
{
    Task BackfillOwnerlessAsync(Guid ownerId);
}
