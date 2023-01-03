using Android.Bluetooth;

using AppoMobi.Maui.BLE.Enums;

namespace AppoMobi.Maui.BLE.Extensions
{
    internal static class GattWriteTypeExtension
    {
        public static CharacteristicWriteType ToCharacteristicWriteType(this GattWriteType writeType)
        {
            if (writeType.HasFlag(GattWriteType.NoResponse))
            {
                return CharacteristicWriteType.WithoutResponse;
            }
            return CharacteristicWriteType.WithResponse;
        }
    }
}
