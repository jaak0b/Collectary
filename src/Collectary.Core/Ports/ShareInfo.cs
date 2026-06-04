using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public record ShareInfo(Guid UserId, string Username, string DisplayName, SharePermission Permission);
