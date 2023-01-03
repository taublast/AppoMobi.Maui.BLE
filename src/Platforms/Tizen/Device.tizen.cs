using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppoMobi.Maui.BLE.Enums;
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{
  public partial class Device
  {
    internal object NativeDevice => throw new PlatformNotSupportedException();

    public virtual void Dispose()
    {
      Adapter?.DisconnectDeviceAsync(this);
    }

    private Task<bool> UpdateRssiNativeAsync() => throw new PlatformNotSupportedException();

    private DeviceState GetState() => throw new PlatformNotSupportedException();

    private Task<IReadOnlyList<Maui.BLE.Service>> GetServicesNativeAsync() => throw new PlatformNotSupportedException();

    private Task<Maui.BLE.Service> GetServiceNativeAsync(Guid id) => throw new PlatformNotSupportedException();

    private Task<int> RequestMtuNativeAsync(int requestValue) => throw new PlatformNotSupportedException();

    private bool UpdateConnectionIntervalNative(ConnectionInterval interval) => throw new PlatformNotSupportedException();
  }
}
