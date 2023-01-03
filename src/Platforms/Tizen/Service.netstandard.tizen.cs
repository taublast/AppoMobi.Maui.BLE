using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{
    public partial class Service : ServiceBase
    {
        internal Guid NativeGuid => throw new PlatformNotSupportedException();

        internal bool NativeIsPrimary => throw new PlatformNotSupportedException();

        internal Task<IList<Maui.BLE.Characteristic>> GetCharacteristicsNativeAsync() => throw new PlatformNotSupportedException();

        internal object NativeService => throw new PlatformNotSupportedException();

        public virtual void Dispose()
        {
        }
    }
}
