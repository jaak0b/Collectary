using Collectary.Core.Ports;
using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public sealed class PreferencesDeviceIdentity : IDeviceIdentity
{
    private readonly object _gate = new();
    private Guid? _cached;

    public Guid DeviceId
    {
        get
        {
            lock (_gate)
            {
                if (_cached is { } cached) return cached;

                if (AppPreferences.Load().DeviceId is { } stored && stored != Guid.Empty)
                {
                    _cached = stored;
                    return stored;
                }

                var minted = Guid.NewGuid();
                AppPreferences.Update(prefs => prefs with { DeviceId = minted });
                _cached = minted;
                return minted;
            }
        }
    }
}
