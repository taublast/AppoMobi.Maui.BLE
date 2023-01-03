using System;
using CoreBluetooth;

using AppoMobi.Maui.BLE.Enums;

namespace AppoMobi.Maui.BLE.Extensions
{
    internal static class CharacteristicWriteTypeExtension
    {
        public static CBCharacteristicWriteType ToNative(this CharacteristicWriteType writeType)
        {
            switch (writeType)
            {
                case CharacteristicWriteType.WithResponse:
                    return CBCharacteristicWriteType.WithResponse;
                case CharacteristicWriteType.WithoutResponse:
                    return CBCharacteristicWriteType.WithoutResponse;
                default:
                    throw new NotImplementedException();
            }
        }
    }

}
