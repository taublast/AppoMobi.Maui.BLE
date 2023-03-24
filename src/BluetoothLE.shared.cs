using AppoMobi.Maui.BLE.Enums;

namespace AppoMobi.Maui.BLE
{

	public partial class BluetoothLE : IBluetoothLE
	{

		private static BluetoothLE CreateImplementation()
		{
			var implementation = new BluetoothLE();
			implementation.Initialize();
			return implementation;
		}

		private readonly Lazy<Maui.BLE.Adapter> _adapter;

		private BluetoothState _state;

		public event EventHandler<BluetoothStateChangedArgs> StateChanged;

		public bool IsAvailable => _state != BluetoothState.Unavailable;

		public bool IsOn => _state == BluetoothState.On;

		public Maui.BLE.Adapter Adapter => _adapter.Value;

		public BluetoothState State
		{
			get => _state;
			protected set
			{
				if (_state == value)
					return;

				var oldState = _state;
				_state = value;

				StateChanged?.Invoke(this, new BluetoothStateChangedArgs(oldState, _state));
			}
		}

		public BluetoothLE()
		{
			_adapter = new Lazy<Maui.BLE.Adapter>(CreateAdapter, System.Threading.LazyThreadSafetyMode.PublicationOnly);

			Initialize();
		}

		public void WarmUp()
		{
			State = GetInitialStateNative();
		}

		public void Initialize()
		{
			InitializeNative();

			WarmUp();
		}

		private Maui.BLE.Adapter CreateAdapter()
		{
			return CreateNativeAdapter();
		}

#if ((NET6_0 || NET7_0) && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)

        public bool HasPermissions => throw new NotImplementedException();

        internal void InitializeNative() { throw new NotImplementedException(); }
        internal Maui.BLE.Adapter CreateNativeAdapter() { throw new NotImplementedException(); }
        internal BluetoothState GetInitialStateNative() { throw new NotImplementedException(); }

#endif



	}

}
