using Collectary.Core.Domain.Import;

namespace Collectary.Core.Ports;

public interface ICsvWorkbookReader
{
    WorkbookData Read(Stream stream);
}
