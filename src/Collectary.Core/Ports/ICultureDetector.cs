using System.Globalization;
using Collectary.Core.Domain.Import;

namespace Collectary.Core.Ports;

public interface ICultureDetector
{
    CultureInfo Detect(IReadOnlyList<IReadOnlyList<WorkbookCell>> rows, IReadOnlyList<CultureInfo> candidates, CultureInfo fallback);
}
