using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AppoMobi.Maui.BLE.Enums;
using AppoMobi.Maui.BLE.Utils;


namespace AppoMobi.Maui.BLE
{

    public partial class BluetoothLE
    {

        public bool HasPermissions
        {
            get
            {
                //todo

                return true;
            }
        }

        private static volatile Handler _handler;
        private BluetoothManager _bluetoothManager;

        /// <summary>
        /// Set this field to force are task builder execute() actions to be invoked on the main app tread one at a time (synchronous queue)
        /// </summary>
        internal static bool ShouldQueueOnMainThread { get; set; } = true;

        private static bool IsMainThread
        {
            get
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    return Looper.MainLooper.IsCurrentThread;
                }

                return Looper.MyLooper() == Looper.MainLooper;
            }
        }

        internal void InitializeNative()
        {
            var ctx = Platform.CurrentActivity; // Application.Context;
            if (!ctx.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
                return;

            var statusChangeReceiver = new BluetoothStatusBroadcastReceiver(state => this.State = state);

            ctx.RegisterReceiver(statusChangeReceiver, new IntentFilter(BluetoothAdapter.ActionStateChanged));

            _bluetoothManager = (BluetoothManager)ctx.GetSystemService(Context.BluetoothService);

            if (ShouldQueueOnMainThread)
            {
                TaskBuilder.MainThreadInvoker = action =>
                {
                    if (IsMainThread)
                    {
                        action();
                    }
                    else
                    {
                        if (_handler == null)
                        {
                            _handler = new Handler(Looper.MainLooper);
                        }

                        _handler.Post(action);
                    }
                };
            }
        }

        internal BluetoothState GetInitialStateNative()
        {
            if (_bluetoothManager != null && _bluetoothManager.Adapter != null)
            {
                return _bluetoothManager.Adapter.State.ToBluetoothState();
            }

            return BluetoothState.Unavailable;
        }

        internal Maui.BLE.Adapter CreateNativeAdapter()
        {
            if (_bluetoothManager != null)
            {
                return new Maui.BLE.Adapter(_bluetoothManager);
            }

            State = BluetoothState.Unavailable;
            return null;
        }
    }
}
