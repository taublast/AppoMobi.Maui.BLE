﻿using System.Diagnostics;

namespace AppoMobi.Maui.BLE
{
	public class LEStream : Stream
	{
		private readonly Task initTask;

		private readonly Device device;
		private Service service;
		private Characteristic receive;
		private Characteristic transmit;
		private Characteristic reset;

		private static readonly Guid ServiceId = new Guid("713D0000-503E-4C75-BA94-3148F18D941E");
		private static readonly Guid ReceiveCharId = new Guid("713D0002-503E-4C75-BA94-3148F18D941E");
		private static readonly Guid TransmitCharId = new Guid("713D0003-503E-4C75-BA94-3148F18D941E");
		private static readonly Guid ResetCharId = new Guid("713D0004-503E-4C75-BA94-3148F18D941E");

		private const int ReadBufferSize = 64 * 1024;
		private readonly List<byte> readBuffer = new List<byte>(ReadBufferSize * 2);
		private readonly AutoResetEvent dataReceived = new AutoResetEvent(false);

		public LEStream(Device device)
		{
			this.device = device;
			initTask = InitializeAsync();
		}

		private async Task InitializeAsync()
		{
			Debug.WriteLine("LEStream: Looking for service " + ServiceId + "...");

			service = await device.GetServiceAsync(ServiceId);

			Debug.WriteLine("LEStream: Got service: " + service.Id);

			Debug.WriteLine("LEStream: Getting characteristics...");

			receive = await service.GetCharacteristicAsync(ReceiveCharId);
			transmit = await service.GetCharacteristicAsync(TransmitCharId);
			reset = await service.GetCharacteristicAsync(ResetCharId);

			Debug.WriteLine("LEStream: Got characteristics");

			receive.ValueUpdated += HandleReceiveValueUpdated;
			await receive.StartUpdatesAsync();
		}

		private async void HandleReceiveValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
		{
			var bytes = e.Characteristic.Value;
			if (bytes == null || bytes.Length == 0)
				return;

			//			Debug.WriteLine ("Receive.Value: " + string.Join (" ", bytes.Select (x => x.ToString ("X2"))));

			lock (readBuffer)
			{
				if (readBuffer.Count + bytes.Length > ReadBufferSize)
					readBuffer.RemoveRange(0, ReadBufferSize / 2);

				readBuffer.AddRange(bytes);
			}

			await reset.WriteAsync(new byte[] { 1 });

			dataReceived.Set();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var t = ReadAsync(buffer, offset, count, CancellationToken.None);
			t.Wait();
			return t.Result;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await initTask;

			while (!cancellationToken.IsCancellationRequested)
			{
				lock (readBuffer)
				{
					if (readBuffer.Count > 0)
					{
						var n = Math.Min(count, readBuffer.Count);
						readBuffer.CopyTo(0, buffer, offset, n);
						readBuffer.RemoveRange(0, n);
						return n;
					}
				}

				await Task.Run(() => dataReceived.WaitOne());
			}

			return 0;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			WriteAsync(buffer, offset, count).Wait();
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (count > 20)
			{
				throw new ArgumentOutOfRangeException(nameof(count), "This function is limited to buffers of 20 bytes and less.");
			}

			await initTask;

			var b = buffer;
			if (offset != 0 || count != b.Length)
			{
				b = new byte[count];
				Array.Copy(buffer, offset, b, 0, count);
			}

			// Write the data
			await transmit.WriteAsync(b);

			// Throttle
			await Task.Delay(TimeSpan.FromMilliseconds(b.Length)); // 1 ms/byte is slow but reliable
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => 0;

		public override long Position
		{
			get { return 0; }
			set { }
		}
	}
}
