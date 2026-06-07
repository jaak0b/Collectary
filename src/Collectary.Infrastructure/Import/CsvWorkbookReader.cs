using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Import;

public sealed class CsvWorkbookReader : ICsvWorkbookReader
{
    private readonly string[] _isoDateFormats =
    {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"
    };

    public CsvWorkbookReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public WorkbookData Read(Stream stream)
    {
        var text = Decode(ReadAllBytes(stream));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            DetectDelimiter = true,
            DetectDelimiterValues = [",", ";", "\t", "|"],
            BadDataFound = null,
            MissingFieldFound = null,
        };

        using var reader = new StringReader(text);
        using var parser = new CsvParser(reader, config);

        var rows = new List<IReadOnlyList<WorkbookCell>>();
        while (parser.Read())
        {
            var record = parser.Record;
            if (record is null) continue;
            rows.Add(record.Select(ToCell).ToList());
        }

        return new WorkbookData(new[] { new WorkbookSheet("CSV", rows) });
    }

    private byte[] ReadAllBytes(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private string Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    private WorkbookCell ToCell(string value)
    {
        if (string.IsNullOrEmpty(value)) return new WorkbookCell(null, WorkbookCellKind.Blank);
        if (LooksLikeInvariantNumber(value)) return new WorkbookCell(value, WorkbookCellKind.Number);
        if (LooksLikeIsoDate(value)) return new WorkbookCell(value, WorkbookCellKind.DateTime);
        return new WorkbookCell(value, WorkbookCellKind.Text);
    }

    private bool LooksLikeInvariantNumber(string value) =>
        decimal.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.InvariantCulture,
            out _);

    private bool LooksLikeIsoDate(string value) =>
        DateTime.TryParseExact(value, _isoDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
