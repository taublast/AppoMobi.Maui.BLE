namespace AppoMobi.Maui.BLE.EventArgs
{
	public class CharacteristicUpdatedEventArgs : System.EventArgs
	{
		public Characteristic Characteristic { get; set; }

		public CharacteristicUpdatedEventArgs(Characteristic characteristic)
		{
			Characteristic = characteristic;
		}
	}
}
