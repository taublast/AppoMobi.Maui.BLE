using System;
using System.Threading.Tasks;
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{
  public partial class Descriptor
  {
    protected object NativeDescriptor => throw new PlatformNotSupportedException();

    protected byte[] NativeValue => throw new PlatformNotSupportedException();

    protected Guid NativeGuid => throw new PlatformNotSupportedException();

    protected Task<byte[]> ReadNativeAsync() => throw new PlatformNotSupportedException();

    protected Task WriteNativeAsync(byte[] data) => throw new PlatformNotSupportedException();
  }
}
