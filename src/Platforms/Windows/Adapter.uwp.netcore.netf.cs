using AppoMobi.Maui.BLE.Enums;
using AppoMobi.Maui.BLE.Exceptions;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace AppoMobi.Maui.BLE
{
	public partial class Adapter
	{
		private BluetoothLEAdvertisementWatcher _bleWatcher;

		private Guid[] _serviceUuids;

		private bool HasServicesFilter => _serviceUuids?.Any() ?? false;

		private List<Guid> _foundIds;

		protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
		{
			_serviceUuids = serviceUuids;

			_bleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = ScanMode.ToNative() };

			Trace.WriteLine("Starting a scan for devices.");

			_foundIds = new List<Guid>();

			_bleWatcher.Received -= DeviceFoundAsync;
			_bleWatcher.Received += DeviceFoundAsync;

			if (_serviceUuids != null)
			{
				var advertisementFilter = new BluetoothLEAdvertisementFilter();
				foreach (var serviceUuid in _serviceUuids)
				{
					advertisementFilter.Advertisement.ServiceUuids.Add(serviceUuid);
				}

				_bleWatcher.AdvertisementFilter = advertisementFilter;
			}

			_bleWatcher.Start();
			return Task.FromResult(true);
		}

		protected void StopScanNative()
		{
			if (_bleWatcher != null)
			{
				Trace.WriteLine("Stopping the scan for devices");
				_bleWatcher.Received -= DeviceFoundAsync;
				_bleWatcher.Stop();
				_bleWatcher = null;
				_foundIds = null;
			}
		}

		protected async Task ConnectToDeviceNativeAsync(Maui.BLE.Device device, ConnectParameters connectParameters, CancellationToken cancellationToken)
		{
			Trace.WriteLine($"Connecting to device with ID:  {device.Id.ToString()}");

			if (!(device.NativeDevice is ObservableBluetoothLEDevice nativeDevice))
				return;

			nativeDevice.PropertyChanged -= Device_ConnectionStatusChanged;
			nativeDevice.PropertyChanged += Device_ConnectionStatusChanged;

			ConnectedDeviceRegistry[device.Id.ToString()] = device;

			await nativeDevice.ConnectAsync();
		}

		private void Device_ConnectionStatusChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (!(sender is ObservableBluetoothLEDevice nativeDevice) || nativeDevice.BluetoothLEDevice == null)
			{
				return;
			}

			if (propertyChangedEventArgs.PropertyName != nameof(nativeDevice.IsConnected))
			{
				return;
			}

			var address = ParseDeviceId(nativeDevice.BluetoothLEDevice.BluetoothAddress).ToString();
			if (nativeDevice.IsConnected && ConnectedDeviceRegistry.TryGetValue(address, out var connectedDevice))
			{
				HandleConnectedDevice(connectedDevice);
				return;
			}

			if (!nativeDevice.IsConnected && ConnectedDeviceRegistry.TryRemove(address, out var disconnectedDevice))
			{
				HandleDisconnectedDevice(false, disconnectedDevice);
			}
		}

		// TODO: protected void DisconnectDeviceNative(IDevice device)
		protected void DisconnectDeviceNative(Maui.BLE.Device device)
		{
			// Windows doesn't support disconnecting, so currently just dispose of the device
			Trace.WriteLine($"Disconnected from device with ID:  {device.Id.ToString()}");

			if (device.NativeDevice is ObservableBluetoothLEDevice nativeDevice)
			{
				((Maui.BLE.Device)device).ClearServices();
				nativeDevice.BluetoothLEDevice.Dispose();
				ConnectedDeviceRegistry.TryRemove(device.Id.ToString(), out _);
			}
		}

		public async Task<Maui.BLE.Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default, bool dontThrowExceptionOnNotFound = false)
		{
			//convert GUID to string and take last 12 characters as MAC address
			var guidString = deviceGuid.ToString("N").Substring(20);
			var bluetoothAddress = Convert.ToUInt64(guidString, 16);
			var nativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);

			if (nativeDevice == null)
			{
				if (dontThrowExceptionOnNotFound == true)
					return null;

				throw new DeviceNotFoundException(deviceGuid);
			}

			var knownDevice = new Maui.BLE.Device(this, nativeDevice, 0, deviceGuid);

			await ConnectToDeviceAsync(knownDevice, cancellationToken: cancellationToken);
			return knownDevice;
		}

		public IReadOnlyList<Maui.BLE.Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
		{
			//currently no way to retrieve paired and connected devices on windows without using an
			//async method.
			Trace.WriteLine("Returning devices connected by this app only");
			return ConnectedDevices;
		}

		/// <summary>
		/// Parses a given advertisement for various stored properties
		/// Currently only parses the manufacturer specific data
		/// </summary>
		/// <param name="adv">The advertisement to parse</param>
		/// <returns>List of generic advertisement records</returns>
		public static List<AdvertisementRecord> ParseAdvertisementData(BluetoothLEAdvertisement adv)
		{
			var advList = adv.DataSections;

			return advList.Select(data => new AdvertisementRecord((AdvertisementRecordType)data.DataType, data.Data?.ToArray())).ToList();
		}

		/// <summary>
		/// Handler for devices found when duplicates are not allowed
		/// </summary>
		/// <param name="watcher">The bluetooth advertisement watcher currently being used</param>
		/// <param name="btAdv">The advertisement recieved by the watcher</param>
		private async void DeviceFoundAsync(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs btAdv)
		{
			var deviceId = ParseDeviceId(btAdv.BluetoothAddress);

			var bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);

			if (bluetoothLeDevice != null)
			{
				var device = new Maui.BLE.Device(this, bluetoothLeDevice, btAdv.RawSignalStrengthInDBm, deviceId, ParseAdvertisementData(btAdv.Advertisement));

				if (DiscoveredDevicesRegistry.TryGetValue(deviceId, out var existingDevice))
				{
					//existing
					Trace.WriteLine("Advertised Peripheral: {0} Id: {1}, Rssi: {2}", existingDevice.Name, existingDevice.Id, btAdv.RawSignalStrengthInDBm);

					existingDevice.Update(btAdv.RawSignalStrengthInDBm, ParseAdvertisementData(btAdv.Advertisement));
					//this.HandleDiscoveredDevice(device);

					existingDevice.MergeOrUpdateAdvertising(device.AdvertisementRecords);
				}
				else
				{
					//new
					bool passed = true;
					//if (HasServicesFilter)
					//{
					//	passed = false;
					//	try
					//	{
					//		var services = await bluetoothLeDevice.GetGattServicesAsync();

					//		if (services.Services.Any())
					//		{
					//			//compare the list of services provided with the _serviceIds being listened for
					//			var items = (from x in services.Services
					//						 join y in _serviceUuids on x.Uuid equals y
					//						 select x)
					//				.ToList();

					//			foreach (var item in items)
					//			{
					//				if (_serviceUuids.Contains(item.Uuid))
					//				{
					//					passed = true;
					//					break;
					//				}
					//			}
					//		}
					//	}
					//	catch (Exception e)
					//	{
					//		Console.WriteLine(e);
					//	}
					//}

					if (passed)
					{
						Trace.WriteLine("Discovered Peripheral: {0} Id: {1}, Rssi: {2}", device.Name, device.Id, btAdv.RawSignalStrengthInDBm);
						this.HandleDiscoveredDevice(device);
					}
					else
					{
						Trace.WriteLine("Filtered Peripheral: {0} Id: {1}, Rssi: {2}", device.Name, device.Id, btAdv.RawSignalStrengthInDBm);
					}
				}
			}


			//{
			//	if (bluetoothLeDevice != null) //make sure advertisement bluetooth address actually returns a device
			//	{
			//		//if there is a filter on devices find the services for the device


			//		if (DiscoveredDevicesRegistry.ContainsKey(device.Id))
			//		{
			//			//try and merge advertising data
			//			var existingDevice = DiscoveredDevicesRegistry[device.Id];

			//			existingDevice.MergeOrUpdateAdvertising(device.AdvertisementRecords);

			//			return;
			//		}


			//	}
			//}
		}

		/// <summary>
		/// Method to parse the bluetooth address as a hex string to a UUID
		/// </summary>
		/// <param name="bluetoothAddress">BluetoothLEDevice native device address</param>
		/// <returns>a GUID that is padded left with 0 and the last 6 bytes are the bluetooth address</returns>
		private static Guid ParseDeviceId(ulong bluetoothAddress)
		{
			var macWithoutColons = bluetoothAddress.ToString("x");
			macWithoutColons = macWithoutColons.PadLeft(12, '0'); //ensure valid length

			var deviceGuid = new byte[16];
			Array.Clear(deviceGuid, 0, 16);

			var macBytes = Enumerable.Range(0, macWithoutColons.Length)
			  .Where(x => x % 2 == 0)
			  .Select(x => Convert.ToByte(macWithoutColons.Substring(x, 2), 16))
			  .ToArray();

			macBytes.CopyTo(deviceGuid, 10);
			return new Guid(deviceGuid);
		}
	}
}
