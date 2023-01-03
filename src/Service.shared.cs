namespace AppoMobi.Maui.BLE
{
	public partial class Service : IDisposable
	{
		private readonly List<Characteristic> _characteristics = new List<Characteristic>();

		public string Name => KnownServices.Lookup(Id).Name;

		public Guid Id => NativeGuid;

		public bool IsPrimary => NativeIsPrimary;

		public Device Device { get; }

		protected Service()
		{
		}

		protected Service(Device device)
		{
			Device = device;
		}

		public async Task<IReadOnlyList<Characteristic>> GetCharacteristicsAsync()
		{
			if (!_characteristics.Any())
			{
				_characteristics.AddRange(await GetCharacteristicsNativeAsync());
			}

			// make a copy here so that the caller cant modify the original list
			return _characteristics.ToList();
		}

		public async Task<Characteristic> GetCharacteristicAsync(Guid id)
		{
			var characteristics = await GetCharacteristicsAsync();
			return characteristics.FirstOrDefault(c => c.Id == id);
		}

#if (NET6_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)

		public virtual void Dispose()
		{
		}

		internal Guid NativeGuid => throw new NotImplementedException();

		internal bool NativeIsPrimary => throw new NotImplementedException();


		internal Task<IList<Characteristic>> GetCharacteristicsNativeAsync()
		{
			throw new NotImplementedException();
		}
#endif
	}
}
