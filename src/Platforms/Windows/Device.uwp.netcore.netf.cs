using AppoMobi.Maui.BLE.Enums;
using Windows.Devices.Bluetooth;

namespace AppoMobi.Maui.BLE
{
	public partial class Device
	{
		internal ObservableBluetoothLEDevice NativeDevice { get; private set; }

		internal Device(Maui.BLE.Adapter adapter, BluetoothLEDevice nativeDevice, int rssi, Guid id, IReadOnlyList<AdvertisementRecord> advertisementRecords = null) : this(adapter)
		{
			NativeDevice = new ObservableBluetoothLEDevice(nativeDevice.DeviceInformation);

			Rssi = rssi;
			Id = id;
			Name = nativeDevice.Name;
			AdvertisementRecords = advertisementRecords;

			NativeDevice.OnNameChanged += (s, name) => { Name = name; };
		}

		public virtual void Dispose()
		{
			Adapter?.DisconnectDeviceAsync(this);
		}

		internal void Update(short btAdvRawSignalStrengthInDBm, IReadOnlyList<AdvertisementRecord> advertisementData)
		{
			this.Rssi = btAdvRawSignalStrengthInDBm;

			MergeOrUpdateAdvertising(advertisementData);

			//this.AdvertisementRecords = advertisementData;
		}

		internal Task<bool> UpdateRssiNativeAsync()
		{
			//No current method to update the Rssi of a device
			//In future implementations, maybe listen for device's advertisements

			Trace.Message("Request RSSI not supported in UWP");

			return Task.FromResult(true);
		}

		private async Task<IReadOnlyList<Maui.BLE.Service>> GetServicesNativeAsync()
		{
			var result = await NativeDevice.BluetoothLEDevice.GetGattServicesAsync(Maui.BLE.BluetoothLE.CacheModeGetServices);
			result.ThrowIfError();

			return result.Services?
				.Select(nativeService => new Maui.BLE.Service(nativeService, this))
				.Cast<Maui.BLE.Service>()
				.ToList();
		}

		private async Task<Maui.BLE.Service> GetServiceNativeAsync(Guid id)
		{
			var result = await NativeDevice.BluetoothLEDevice.GetGattServicesForUuidAsync(id, Maui.BLE.BluetoothLE.CacheModeGetServices);
			result.ThrowIfError();

			var nativeService = result.Services?.FirstOrDefault();
			return nativeService != null ? new Maui.BLE.Service(nativeService, this) : null;
		}

		private DeviceState GetState()
		{
			if (NativeDevice.IsConnected)
			{
				return DeviceState.Connected;
			}

			return NativeDevice.IsPaired ? DeviceState.Limited : DeviceState.Disconnected;
		}

		private Task<int> RequestMtuNativeAsync(int requestValue)
		{
			Trace.Message("Request MTU not supported in UWP");
			return Task.FromResult(-1);
		}

		private bool UpdateConnectionIntervalNative(ConnectionInterval interval)
		{
			Trace.Message("Update Connection Interval not supported in UWP");
			return false;
		}

		internal void MergeOrUpdateAdvertising(IReadOnlyList<AdvertisementRecord> advertisementRecords)
		{
			var adverts = this.AdvertisementRecords.ToList();

			foreach (var adv in advertisementRecords)
			{
				var matcing = adverts.FirstOrDefault(x => x.Type.Equals(adv.Type));

				if (matcing != null)
					adverts.Remove(matcing);

				adverts.Add(adv);
			}

			this.AdvertisementRecords = adverts;
		}
	}
}
