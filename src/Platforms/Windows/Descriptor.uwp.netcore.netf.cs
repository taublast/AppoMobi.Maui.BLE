using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace AppoMobi.Maui.BLE
{
	public partial class Descriptor
	{
		/// <summary>
		/// The locally stored value of a descriptor updated after a
		/// notification or a read
		/// </summary>
		private byte[] _value;

		protected Guid NativeGuid => NativeDescriptor.Uuid;

		protected byte[] NativeValue => _value ?? new byte[0];

		protected GattDescriptor NativeDescriptor { get; private set; }

		public Descriptor(GattDescriptor nativeDescriptor, Maui.BLE.Characteristic characteristic) : this(characteristic)
		{
			NativeDescriptor = nativeDescriptor;
		}

		protected async Task<byte[]> ReadNativeAsync()
		{
			var readResult = await NativeDescriptor.ReadValueAsync(Maui.BLE.BluetoothLE.CacheModeDescriptorRead);
			return _value = readResult.GetValueOrThrowIfError();
		}

		protected async Task WriteNativeAsync(byte[] data)
		{
			var result = await NativeDescriptor.WriteValueWithResultAsync(CryptographicBuffer.CreateFromByteArray(data));
			result.ThrowIfError();
		}
	}
}
