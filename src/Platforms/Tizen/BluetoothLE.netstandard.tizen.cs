using System;
using AppoMobi.Maui.BLE.Enums;
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{
    public partial class BluetoothLE
    {
        internal Maui.BLE.Adapter CreateNativeAdapter() => throw new PlatformNotSupportedException();

        internal BluetoothState GetInitialStateNative() => throw new PlatformNotSupportedException();

        internal void InitializeNative() => throw new PlatformNotSupportedException();

        public bool HasPermissions
        {
            get
            {
                return true;
            }
        }
    }
}
