namespace AppoMobi.Maui.BLE
{
	public static class Trace
	{
		public static Action<string, object[]> TraceImplementation { get; set; }

		public static void WriteLine(string format, params object[] args)
		{
			var text = string.Format(format, args);

			//Debug.WriteLine($"[BLE LIB] {text}");

			try
			{
				TraceImplementation?.Invoke(format, args);
			}
			catch
			{
			}
		}
	}
}
