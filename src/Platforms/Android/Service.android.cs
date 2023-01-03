using Android.Bluetooth;

namespace AppoMobi.Maui.BLE
{
    public partial class Service
    {
        private readonly BluetoothGatt _gatt;

        private readonly IGattCallback _gattCallback;

        internal Guid NativeGuid => Guid.ParseExact(NativeService.Uuid.ToString(), "d");

        internal bool NativeIsPrimary => NativeService.Type == GattServiceType.Primary;

        internal BluetoothGattService NativeService { get; private set; }

        internal Service(BluetoothGattService nativeService, BluetoothGatt gatt, IGattCallback gattCallback, Maui.BLE.Device device) : this(device)
        {
            NativeService = nativeService;

            _gatt = gatt;
            _gattCallback = gattCallback;
        }

        internal Task<IList<Maui.BLE.Characteristic>> GetCharacteristicsNativeAsync()
        {
            return Task.FromResult<IList<Maui.BLE.Characteristic>>(
              NativeService.Characteristics.Select(characteristic => new Maui.BLE.Characteristic(characteristic, _gatt, _gattCallback, this))
              .Cast<Maui.BLE.Characteristic>().ToList());
        }

        public virtual void Dispose()
        {

        }
    }
}
