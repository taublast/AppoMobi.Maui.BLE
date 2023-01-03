using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace AppoMobi.Maui.BLE
{
	public partial class Service
	{
		internal Guid NativeGuid => NativeService.Uuid;

		//method to get parent devices to check if primary is obsolete
		//return true as a placeholder
		internal bool NativeIsPrimary => true;

		internal GattDeviceService NativeService { get; private set; }

		internal Service(GattDeviceService nativeService, Maui.BLE.Device device) : this(device)
		{
			NativeService = nativeService;
		}

		internal async Task<IList<Maui.BLE.Characteristic>> GetCharacteristicsNativeAsync()
		{
			var accessRequestResponse = await NativeService.RequestAccessAsync();

			// Returns Allowed
			if (accessRequestResponse != DeviceAccessStatus.Allowed)
			{
				throw new Exception("Access to service " + NativeService.Uuid.ToString() + " was disallowed w/ response: " + accessRequestResponse);
			}

			var result = await NativeService.GetCharacteristicsAsync(Maui.BLE.BluetoothLE.CacheModeGetCharacteristics);
			result.ThrowIfError();

			return result.Characteristics?
			  .Select(nativeChar => new Maui.BLE.Characteristic(nativeChar, this))
			  .Cast<Maui.BLE.Characteristic>()
			  .ToList();
		}

		public virtual void Dispose()
		{
			NativeService?.Dispose();
		}
	}
}
