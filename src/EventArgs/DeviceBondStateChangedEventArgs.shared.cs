using AppoMobi.Maui.BLE.Enums;

namespace AppoMobi.Maui.BLE.EventArgs
{
	public class DeviceBondStateChangedEventArgs : System.EventArgs
	{
		public Device Device { get; set; }

		public DeviceBondState State { get; set; }
	}
}
