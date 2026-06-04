using System.Text.RegularExpressions;
using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.ListCells;

public class RichTextListCellViewModel : ListCellViewModelBase
{
    public string Preview { get; }

    public RichTextListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Preview = StripMarkdown(source.ToString());
    }

    private static string StripMarkdown(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var text = Regex.Replace(input, @"[#*_`\[\]()>~]", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length > 80 ? text[..80] + "…" : text;
    }
}
