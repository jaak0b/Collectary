using Collectary.Core.Domain.Import;

namespace Collectary.Core.Ports;

public interface IExcelWorkbookReader
{
    WorkbookData Read(Stream stream);
}
