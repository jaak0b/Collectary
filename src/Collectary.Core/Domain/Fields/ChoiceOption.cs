namespace Collectary.Core.Domain.Fields;

public class ChoiceOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
