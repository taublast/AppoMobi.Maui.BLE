using AppoMobi.Maui.BLE.Enums;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace AppoMobi.Maui.BLE
{
	public partial class Characteristic
	{
		/// <summary>
		/// Value of the characteristic to be stored locally after
		/// update notification or read
		/// </summary>
		private byte[] _value;

		protected Guid NativeGuid => NativeCharacteristic.Uuid;

		protected string NativeUuid => NativeCharacteristic.Uuid.ToString();

		protected byte[] NativeValue => _value ?? new byte[0]; // return empty array if value is equal to null

		protected string NativeName => string.IsNullOrEmpty(NativeCharacteristic.UserDescription) ? KnownCharacteristics.Lookup(Id).Name : NativeCharacteristic.UserDescription;

		protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.CharacteristicProperties;

		protected GattCharacteristic NativeCharacteristic { get; private set; }

		public Characteristic(GattCharacteristic nativeCharacteristic, Maui.BLE.Service service) : this(service)
		{
			NativeCharacteristic = nativeCharacteristic;
		}

		protected async Task<IReadOnlyList<Maui.BLE.Descriptor>> GetDescriptorsNativeAsync()
		{
			var descriptorsResult = await NativeCharacteristic.GetDescriptorsAsync(Maui.BLE.BluetoothLE.CacheModeGetDescriptors);
			descriptorsResult.ThrowIfError();

			return descriptorsResult.Descriptors?
			  .Select(nativeDescriptor => new Maui.BLE.Descriptor(nativeDescriptor, this))
			  .Cast<Maui.BLE.Descriptor>()
			  .ToList();
		}

		protected async Task<byte[]> ReadNativeAsync()
		{
			var readResult = await NativeCharacteristic.ReadValueAsync(Maui.BLE.BluetoothLE.CacheModeCharacteristicRead);
			return _value = readResult.GetValueOrThrowIfError();
		}

		protected async Task StartUpdatesNativeAsync()
		{
			NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
			NativeCharacteristic.ValueChanged += OnCharacteristicValueChanged;

			var result = await NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
			result.ThrowIfError();
		}

		protected async Task StopUpdatesNativeAsync()
		{
			NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;

			var result = await NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
			result.ThrowIfError();
		}

		protected async Task<bool> WriteNativeAsync(byte[] data, CharacteristicWriteType writeType)
		{
			var result = await NativeCharacteristic.WriteValueWithResultAsync(
			  CryptographicBuffer.CreateFromByteArray(data),
			  writeType == CharacteristicWriteType.WithResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse);

			result.ThrowIfError();
			return true;
		}

		/// <summary>
		/// Handler for when the characteristic value is changed. Updates the
		/// stored value
		/// </summary>
		private void OnCharacteristicValueChanged(object sender, GattValueChangedEventArgs e)
		{
			_value = e.CharacteristicValue?.ToArray(); //add value to array
			ValueUpdated?.Invoke(this, new CharacteristicUpdatedEventArgs(this));
		}
	}
}
