
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{
	public partial class Descriptor
	{
		private string _name;

		public string Name => _name ?? (_name = KnownDescriptors.Lookup(Id).Name);

		public byte[] Value => NativeValue;

		public Guid Id => NativeGuid;

		public Characteristic Characteristic { get; }

		protected Descriptor()
		{
		}

		protected Descriptor(Characteristic characteristic)
		{
			Characteristic = characteristic;
		}

		public Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
		{
			return ReadNativeAsync();
		}

		public Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			return WriteNativeAsync(data);
		}

#if ((NET6_0 || NET7_0) && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)

        protected Guid NativeGuid => throw new PlatformNotSupportedException();

        protected byte[] NativeValue => throw new PlatformNotSupportedException();

        protected async Task<byte[]> ReadNativeAsync() { throw new PlatformNotSupportedException(); }

        protected Task WriteNativeAsync(byte[] data) { throw new PlatformNotSupportedException(); }

#endif

	}
}
