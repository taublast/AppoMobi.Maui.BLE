using Android.Bluetooth;

namespace AppoMobi.Maui.BLE.EventArgs
{
    public class CharacteristicReadCallbackEventArgs
    {
        public BluetoothGattCharacteristic Characteristic { get; }

        public CharacteristicReadCallbackEventArgs(BluetoothGattCharacteristic characteristic)
        {
            Characteristic = characteristic;
        }
    }
}
