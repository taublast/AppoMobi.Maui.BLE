using AppoMobi.Maui.BLE.Enums;
using AppoMobi.Maui.BLE.Exceptions;
using AppoMobi.Maui.BLE.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{


	public partial class Adapter   //: IAdapter
	{
		private CancellationTokenSource _scanCancellationTokenSource;
		private volatile bool _isScanning;
		private Func<Device, bool> _currentScanDeviceFilter;

		public event EventHandler<DeviceEventArgs> DeviceAdvertised;

		public event EventHandler<DeviceEventArgs> DeviceDiscovered;

		public event EventHandler<DeviceEventArgs> DeviceConnected;

		public event EventHandler<DeviceEventArgs> DeviceDisconnected;

		public event EventHandler<DeviceErrorEventArgs> DeviceConnectionLost;

		public event EventHandler<DeviceErrorEventArgs> DeviceConnectionError;

		public event EventHandler ScanTimeoutElapsed;

		public bool IsScanning
		{
			get => _isScanning;
			private set => _isScanning = value;
		}

		public int ScanTimeout { get; set; } = 30000;
		public ScanMode ScanMode { get; set; } = ScanMode.LowPower;

		protected ConcurrentDictionary<Guid, Device> DiscoveredDevicesRegistry { get; } = new ConcurrentDictionary<Guid, Device>();

		public virtual IReadOnlyList<Device> DiscoveredDevices => DiscoveredDevicesRegistry.Values.ToList();

		/// <summary>
		/// Used to store all connected devices
		/// </summary>
		public ConcurrentDictionary<string, Device> ConnectedDeviceRegistry { get; } = new ConcurrentDictionary<string, Device>();

		public IReadOnlyList<Device> ConnectedDevices => ConnectedDeviceRegistry.Values.ToList();

		public async Task StartScanningForDevicesAsync(Guid[] serviceUuids = null, Func<Device, bool> deviceFilter = null,
			bool allowDuplicatesKey = false, CancellationToken cancellationToken = default)
		{
			if (IsScanning)
			{
				Trace.WriteLine("Adapter: Already scanning!");
				return;
			}

			IsScanning = true;
			serviceUuids = serviceUuids ?? new Guid[0];
			_currentScanDeviceFilter = deviceFilter ?? (d => true);
			_scanCancellationTokenSource = new CancellationTokenSource();

			try
			{
				DiscoveredDevicesRegistry.Clear();

				using (cancellationToken.Register(() => _scanCancellationTokenSource?.Cancel()))
				{
					await StartScanningForDevicesNativeAsync(serviceUuids, allowDuplicatesKey,
						_scanCancellationTokenSource.Token);

					await Task.Delay(ScanTimeout, _scanCancellationTokenSource.Token);

					Trace.WriteLine($"Adapter: Scan timeout has elapsed ({ScanTimeout}ms).");
					CleanupScan();
					ScanTimeoutElapsed?.Invoke(this, new System.EventArgs());
				}
			}
			catch (TaskCanceledException)
			{
				CleanupScan();
				Trace.WriteLine("Adapter: Scan was cancelled.");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				IsScanning = false;
			}
		}

		public Task StopScanningForDevicesAsync()
		{
			if (_scanCancellationTokenSource != null && !_scanCancellationTokenSource.IsCancellationRequested)
			{
				_scanCancellationTokenSource.Cancel();
			}
			else
			{
				Trace.WriteLine("Adapter: Already cancelled scan.");
			}

			return Task.FromResult(0);
		}

		public async Task ConnectToDeviceAsync(Device device,
			ConnectParameters connectParameters = default,
			CancellationToken cancellationToken = default)
		{
			if (device == null)
				throw new ArgumentNullException(nameof(device));

			if (device.State == DeviceState.Connected)
				return;

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				await TaskBuilder.FromEvent<bool, EventHandler<DeviceEventArgs>, EventHandler<DeviceErrorEventArgs>>(
					execute: () =>
					{
						ConnectToDeviceNativeAsync(device, connectParameters, cts.Token);
					},

					getCompleteHandler: (complete, reject) => (sender, args) =>
					{
						if (args.Device.Id == device.Id)
						{
							Trace.WriteLine("ConnectToDeviceAsync Connected: {0} {1}", args.Device.Id, args.Device.Name);
							complete(true);
						}
					},

					subscribeComplete: handler => DeviceConnected += handler,
					unsubscribeComplete: handler => DeviceConnected -= handler,

					getRejectHandler: reject => (sender, args) =>
					{
						if (args.Device?.Id == device.Id)
						{
							Trace.WriteLine("ConnectAsync Error: {0} {1}", args.Device?.Id, args.Device?.Name);
							reject(new DeviceConnectionException((Guid)args.Device?.Id, args.Device?.Name,
								args.ErrorMessage));
						}
					},

					subscribeReject: handler => DeviceConnectionError += handler,
					unsubscribeReject: handler => DeviceConnectionError -= handler,
					token: cts.Token);
			}
		}

		public Task DisconnectDeviceAsync(Device device)
		{
			if (!ConnectedDevices.Contains(device))
			{
				Trace.WriteLine("Disconnect async: device {0} not in the list of connected devices.", device.Name);
				return Task.FromResult(false);
			}

			return TaskBuilder.FromEvent<bool, EventHandler<DeviceEventArgs>, EventHandler<DeviceErrorEventArgs>>(
			   execute: () => DisconnectDeviceNative(device),

			   getCompleteHandler: (complete, reject) => ((sender, args) =>
			   {
				   if (args.Device.Id == device.Id)
				   {
					   Trace.WriteLine("DisconnectAsync Disconnected: {0} {1}", args.Device.Id, args.Device.Name);
					   complete(true);
				   }
			   }),

			   subscribeComplete: handler => DeviceDisconnected += handler,
			   unsubscribeComplete: handler => DeviceDisconnected -= handler,

			   getRejectHandler: reject => ((sender, args) =>
			   {
				   if (args.Device.Id == device.Id)
				   {
					   Trace.WriteLine("DisconnectAsync", "Disconnect Error: {0} {1}", args.Device?.Id, args.Device?.Name);
					   reject(new Exception("Disconnect operation exception"));
				   }
			   }),

			   subscribeReject: handler => DeviceConnectionError += handler,
			   unsubscribeReject: handler => DeviceConnectionError -= handler);
		}

		private void CleanupScan()
		{
			Trace.WriteLine("Adapter: Stopping the scan for devices.");
			StopScanNative();

			if (_scanCancellationTokenSource != null)
			{
				_scanCancellationTokenSource.Dispose();
				_scanCancellationTokenSource = null;
			}

			IsScanning = false;
		}

		public void HandleDiscoveredDevice(Device device)
		{
			DeviceAdvertised?.Invoke(this, new DeviceEventArgs { Device = device });

			// TODO (sms): check equality implementation of device
			if (DiscoveredDevicesRegistry.ContainsKey(device.Id))
				return;

			bool isNew = !DiscoveredDevicesRegistry.ContainsKey(device.Id);

			DiscoveredDevicesRegistry[device.Id] = device;

			if (_currentScanDeviceFilter != null && !_currentScanDeviceFilter(device))
				return;

			if (isNew)
			{
				Debug.WriteLine($"[BLE] DeviceDiscovered {device.Id}");
				DeviceDiscovered?.Invoke(this, new DeviceEventArgs { Device = device });
			}
		}

		public void HandleConnectedDevice(Device device)
		{
			DeviceConnected?.Invoke(this, new DeviceEventArgs { Device = device });
		}

		public void HandleDisconnectedDevice(bool disconnectRequested, Device device)
		{
			if (disconnectRequested)
			{
				Trace.WriteLine("DisconnectedPeripheral by user: {0}", device.Name);
				DeviceDisconnected?.Invoke(this, new DeviceEventArgs { Device = device });
			}
			else
			{
				Trace.WriteLine("DisconnectedPeripheral by lost signal: {0}", device.Name);
				DeviceConnectionLost?.Invoke(this, new DeviceErrorEventArgs { Device = device });

				if (DiscoveredDevicesRegistry.TryRemove(device.Id, out _))
				{
					Trace.WriteLine("Removed device from discovered devices list: {0}", device.Name);
				}
			}
		}

		public void HandleConnectionFail(Device device, string errorMessage)
		{
			Trace.WriteLine("Failed to connect peripheral {0}: {1}", device.Id, device.Name);
			DeviceConnectionError?.Invoke(this, new DeviceErrorEventArgs
			{
				Device = device,
				ErrorMessage = errorMessage
			});
		}


#if ((NET6_0 || NET7_0) && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)
		protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
		{
			throw new PlatformNotSupportedException();
		}
		protected Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters,
			CancellationToken cancellationToken)
		{
			throw new PlatformNotSupportedException();
		}
		protected void DisconnectDeviceNative(Device device) { throw new PlatformNotSupportedException(); }
		protected void StopScanNative() { new PlatformNotSupportedException(); }

#endif

		public Adapter()
		{
		}
	}
}
