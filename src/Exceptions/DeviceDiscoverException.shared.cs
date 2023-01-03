namespace AppoMobi.Maui.BLE.Exceptions
{
  public class DeviceDiscoverException : Exception
  {
    public DeviceDiscoverException() : base("Could not find the specific device.")
    {
    }
  }
}
