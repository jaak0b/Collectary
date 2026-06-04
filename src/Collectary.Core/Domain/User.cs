namespace Collectary.Core.Domain;

public class User : DomainObject
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
