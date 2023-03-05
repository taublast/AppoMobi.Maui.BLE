using AppoMobi.Maui.BLE.Enums;
using AppoMobi.Maui.BLE.EventArgs;
using System.Text;
using PlatformNotSupportedException = AppoMobi.Maui.BLE.Exceptions.PlatformNotSupportedException;

namespace AppoMobi.Maui.BLE
{
	public partial class Characteristic
	{
		private IReadOnlyList<Descriptor> _descriptors;

		private CharacteristicWriteType _writeType = CharacteristicWriteType.Default;

		public event EventHandler<CharacteristicUpdatedEventArgs> ValueUpdated;

		public Guid Id => NativeGuid;

		public string Uuid => NativeUuid;

		public byte[] Value => NativeValue;

		public CharacteristicPropertyType Properties => NativeProperties;

		public string Name => NativeName;

		public Service Service { get; }

		public CharacteristicWriteType WriteType
		{
			get => _writeType;
			set
			{
				if (value == CharacteristicWriteType.WithResponse && !Properties.HasFlag(CharacteristicPropertyType.Write) ||
					value == CharacteristicWriteType.WithoutResponse && !Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse))
				{
					throw new InvalidOperationException($"Write type {value} is not supported");
				}

				_writeType = value;
			}
		}

		public bool CanRead => Properties.HasFlag(CharacteristicPropertyType.Read);

		public bool CanUpdate => Properties.HasFlag(CharacteristicPropertyType.Notify) |
								 Properties.HasFlag(CharacteristicPropertyType.Indicate);

		public bool CanWrite => Properties.HasFlag(CharacteristicPropertyType.Write) |
								Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse);

		public string StringValue
		{
			get
			{
				var val = Value;
				if (val == null)
					return string.Empty;

				return Encoding.UTF8.GetString(val, 0, val.Length);
			}
		}

		protected Characteristic()
		{
		}

		protected Characteristic(Service service)
		{
			Service = service;
		}

		public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
		{
			if (!CanRead)
			{
				throw new InvalidOperationException("Characteristic does not support read.");
			}

			Trace.WriteLine("Characteristic.ReadAsync");
			return await ReadNativeAsync();
		}

		public async Task<bool> WriteAsync(byte[] data, CancellationToken cancellationToken = default)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			if (!CanWrite)
			{
				throw new InvalidOperationException("Characteristic does not support write.");
			}

			var writeType = GetWriteType();

			Trace.WriteLine("Characteristic.WriteAsync");
			return await WriteNativeAsync(data, writeType);
		}

		private CharacteristicWriteType GetWriteType()
		{
			if (WriteType != CharacteristicWriteType.Default)
				return WriteType;

			return Properties.HasFlag(CharacteristicPropertyType.Write) ?
				CharacteristicWriteType.WithResponse :
				CharacteristicWriteType.WithoutResponse;
		}

		public Task StartUpdatesAsync()
		{
			if (!CanUpdate)
			{
				throw new InvalidOperationException("Characteristic does not support update.");
			}

			Trace.WriteLine("Characteristic.StartUpdates");
			return StartUpdatesNativeAsync();
		}

		public Task StopUpdatesAsync()
		{
			if (!CanUpdate)
			{
				throw new InvalidOperationException("Characteristic does not support update.");
			}

			return StopUpdatesNativeAsync();
		}

		public async Task<IReadOnlyList<Descriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
		{
			return _descriptors ?? (_descriptors = await GetDescriptorsNativeAsync());
		}

		public async Task<Descriptor> GetDescriptorAsync(Guid id, CancellationToken cancellationToken = default)
		{
			var descriptors = await GetDescriptorsAsync(cancellationToken).ConfigureAwait(false);
			return descriptors.FirstOrDefault(d => d.Id == id);
		}

#if (NET6_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)
		protected Guid NativeGuid => throw new PlatformNotSupportedException();

		protected string NativeUuid => throw new PlatformNotSupportedException();

		protected byte[] NativeValue => throw new PlatformNotSupportedException();

		protected CharacteristicPropertyType NativeProperties => throw new PlatformNotSupportedException();

		protected string NativeName => throw new PlatformNotSupportedException();

		protected async Task<byte[]> ReadNativeAsync()
		{
			throw new PlatformNotSupportedException();
		}

		protected async Task<bool> WriteNativeAsync(byte[] data, CharacteristicWriteType writeType)
		{
			throw new PlatformNotSupportedException();
		}

		protected async Task StopUpdatesNativeAsync()
		{
			throw new PlatformNotSupportedException();
		}

		protected async Task StartUpdatesNativeAsync()
		{
			throw new PlatformNotSupportedException();
		}
		protected Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync()
		{
			throw new PlatformNotSupportedException();
		}

#endif

	}
}
