using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using AppoMobi.Maui.BLE.Enums;
using AppoMobi.Maui.BLE.Exceptions;
using Java.Util;

namespace AppoMobi.Maui.BLE
{
	public partial class Adapter
	{
		private readonly BluetoothManager _bluetoothManager;
		private readonly BluetoothAdapter _bluetoothAdapter;
		private readonly Api18BleScanCallback _api18ScanCallback;
		private readonly Api21BleScanCallback _api21ScanCallback;

		public Adapter(BluetoothManager bluetoothManager)
		{
			_bluetoothManager = bluetoothManager;
			_bluetoothAdapter = bluetoothManager.Adapter;

			// TODO: bonding
			//var bondStatusBroadcastReceiver = new BondStatusBroadcastReceiver();
			//Application.Context.RegisterReceiver(bondStatusBroadcastReceiver,
			//    new IntentFilter(BluetoothDevice.ActionBondStateChanged));

			////forward events from broadcast receiver
			//bondStatusBroadcastReceiver.BondStateChanged += (s, args) =>
			//{
			//    //DeviceBondStateChanged(this, args);
			//};

			if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
			{
				_api21ScanCallback = new Api21BleScanCallback(this);
			}
			else
			{
				_api18ScanCallback = new Api18BleScanCallback(this);
			}
		}

		protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
			{
				StartScanningOld(serviceUuids);
			}
			else
			{
				StartScanningNew(serviceUuids);
			}

			return Task.FromResult(true);
		}

		private void StartScanningOld(Guid[] serviceUuids)
		{
			var hasFilter = serviceUuids?.Any() ?? false;
			UUID[] uuids = null;
			if (hasFilter)
			{
				uuids = serviceUuids.Select(u => UUID.FromString(u.ToString())).ToArray();
			}

			Trace.WriteLine("Adapter < 21: Starting a scan for devices.");
#pragma warning disable 618
			_bluetoothAdapter.StartLeScan(uuids, _api18ScanCallback);
#pragma warning restore 618
		}

		private void StartScanningNew(Guid[] serviceUuids)
		{
			var hasFilter = serviceUuids?.Any() ?? false;
			List<ScanFilter> scanFilters = null;

			if (hasFilter)
			{
				scanFilters = new List<ScanFilter>();
				foreach (var serviceUuid in serviceUuids)
				{
					var sfb = new ScanFilter.Builder();
					sfb.SetServiceUuid(ParcelUuid.FromString(serviceUuid.ToString()));
					scanFilters.Add(sfb.Build());
				}
			}

			var ssb = new ScanSettings.Builder();
			ssb.SetScanMode(ScanMode.ToNative());

			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			{
				// enable Bluetooth 5 Advertisement Extensions on Android 8.0 and above
				ssb.SetLegacy(false);
			}
			//ssb.SetCallbackType(ScanCallbackType.AllMatches);

			if (_bluetoothAdapter.BluetoothLeScanner != null)
			{
				Trace.WriteLine($"Adapter >=21: Starting a scan for devices. ScanMode: {ScanMode}");
				if (hasFilter)
				{
					Trace.WriteLine($"ScanFilters: {string.Join(", ", serviceUuids)}");
				}

				_bluetoothAdapter.BluetoothLeScanner.StartScan(scanFilters, ssb.Build(), _api21ScanCallback);
			}
			else
			{
				Trace.WriteLine("Adapter >= 21: Scan failed. Bluetooth is probably off");
			}
		}

		protected void StopScanNative()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
			{
				Trace.WriteLine("Adapter < 21: Stopping the scan for devices.");
#pragma warning disable 618
				_bluetoothAdapter.StopLeScan(_api18ScanCallback);
#pragma warning restore 618
			}
			else
			{
				Trace.WriteLine("Adapter >= 21: Stopping the scan for devices.");
				_bluetoothAdapter.BluetoothLeScanner?.StopScan(_api21ScanCallback);
			}
		}

		protected Task ConnectToDeviceNativeAsync(Maui.BLE.Device device, ConnectParameters connectParameters,
			CancellationToken cancellationToken)
		{
			((Maui.BLE.Device)device).Connect(connectParameters, cancellationToken);
			return Task.CompletedTask;
		}

		protected void DisconnectDeviceNative(Maui.BLE.Device device)
		{
			//make sure everything is disconnected
			((Maui.BLE.Device)device).Disconnect();
		}

		public async Task<Maui.BLE.Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default(ConnectParameters), CancellationToken cancellationToken = default(CancellationToken), bool dontThrowExceptionOnNotFound = false)
		{
			var macBytes = deviceGuid.ToByteArray().Skip(10).Take(6).ToArray();
			var nativeDevice = _bluetoothAdapter.GetRemoteDevice(macBytes);

			if (nativeDevice == null)
			{
				if (dontThrowExceptionOnNotFound == true)
					return null;

				throw new DeviceNotFoundException(deviceGuid);
			}

			var device = new Maui.BLE.Device(this, nativeDevice, null, 0, new byte[] { });

			await ConnectToDeviceAsync(device, connectParameters, cancellationToken);
			return device;
		}

		public IReadOnlyList<Maui.BLE.Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
		{
			if (services != null)
			{
				Trace.WriteLine("Caution: GetSystemConnectedDevices does not take into account the 'services' parameter on Android.");
			}

			//add dualMode type too as they are BLE too ;)
			var connectedDevices = _bluetoothManager.GetConnectedDevices(ProfileType.Gatt).Where(d => d.Type == BluetoothDeviceType.Le || d.Type == BluetoothDeviceType.Dual);

			var bondedDevices = _bluetoothAdapter.BondedDevices.Where(d => d.Type == BluetoothDeviceType.Le || d.Type == BluetoothDeviceType.Dual);

			return connectedDevices.Union(bondedDevices, new DeviceComparer()).Select(d => new Maui.BLE.Device(this, d, null, 0)).Cast<Maui.BLE.Device>().ToList();
		}

		private class DeviceComparer : IEqualityComparer<BluetoothDevice>
		{
			public bool Equals(BluetoothDevice x, BluetoothDevice y)
			{
				return x.Address == y.Address;
			}

			public int GetHashCode(BluetoothDevice obj)
			{
				return obj.GetHashCode();
			}
		}

		public class Api18BleScanCallback : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
		{
			private readonly Maui.BLE.Adapter _adapter;

			public Api18BleScanCallback(Maui.BLE.Adapter adapter)
			{
				_adapter = adapter;
			}

			public void OnLeScan(BluetoothDevice bleDevice, int rssi, byte[] scanRecord)
			{
				Trace.WriteLine("Adapter.LeScanCallback: " + bleDevice.Name);

				_adapter.HandleDiscoveredDevice(new Maui.BLE.Device(_adapter, bleDevice, null, rssi, scanRecord));
			}
		}

		public class Api21BleScanCallback : ScanCallback
		{
			private readonly Maui.BLE.Adapter _adapter;
			public Api21BleScanCallback(Maui.BLE.Adapter adapter)
			{
				_adapter = adapter;
			}

			public override void OnScanFailed(ScanFailure errorCode)
			{
				Trace.WriteLine("Adapter: Scan failed with code {0}", errorCode);
				base.OnScanFailed(errorCode);
			}

			public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
			{
				base.OnScanResult(callbackType, result);

				/* Might want to transition to parsing the API21+ ScanResult, but sort of a pain for now
                List<AdvertisementRecord> records = new List<AdvertisementRecord>();
                records.Add(new AdvertisementRecord(AdvertisementRecordType.Flags, BitConverter.GetBytes(result.ScanRecord.AdvertiseFlags)));
                if (!string.IsNullOrEmpty(result.ScanRecord.DeviceName))
                {
                    records.Add(new AdvertisementRecord(AdvertisementRecordType.CompleteLocalName, Encoding.UTF8.GetBytes(result.ScanRecord.DeviceName)));
                }
                for (int i = 0; i < result.ScanRecord.ManufacturerSpecificData.Size(); i++)
                {
                    int key = result.ScanRecord.ManufacturerSpecificData.KeyAt(i);
                    var arr = result.ScanRecord.GetManufacturerSpecificData(key);
                    byte[] data = new byte[arr.Length + 2];
                    BitConverter.GetBytes((ushort)key).CopyTo(data,0);
                    arr.CopyTo(data, 2);
                    records.Add(new AdvertisementRecord(AdvertisementRecordType.ManufacturerSpecificData, data));
                }

                foreach(var uuid in result.ScanRecord.ServiceUuids)
                {
                    records.Add(new AdvertisementRecord(AdvertisementRecordType.UuidsIncomplete128Bit, uuid.Uuid.));
                }

                foreach(var key in result.ScanRecord.ServiceData.Keys)
                {
                    records.Add(new AdvertisementRecord(AdvertisementRecordType.ServiceData, result.ScanRecord.ServiceData));
                }*/

				var device = new Maui.BLE.Device(_adapter, result.Device, null, result.Rssi, result.ScanRecord.GetBytes());

				//Device device;
				//if (result.ScanRecord.ManufacturerSpecificData.Size() > 0)
				//{
				//    int key = result.ScanRecord.ManufacturerSpecificData.KeyAt(0);
				//    byte[] mdata = result.ScanRecord.GetManufacturerSpecificData(key);
				//    byte[] mdataWithKey = new byte[mdata.Length + 2];
				//    BitConverter.GetBytes((ushort)key).CopyTo(mdataWithKey, 0);
				//    mdata.CopyTo(mdataWithKey, 2);
				//    device = new Device(result.Device, null, null, result.Rssi, mdataWithKey);
				//}
				//else
				//{
				//    device = new Device(result.Device, null, null, result.Rssi, new byte[0]);
				//}

				_adapter.HandleDiscoveredDevice(device);
			}
		}
	}
}
