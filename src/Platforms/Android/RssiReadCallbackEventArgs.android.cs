using System;

namespace AppoMobi.Maui.BLE.EventArgs
{
  public class RssiReadCallbackEventArgs : System.EventArgs
  {
    public Exception Error { get; }

    public int Rssi { get; }

    public RssiReadCallbackEventArgs(Exception error, int rssi)
    {
      Error = error;
      Rssi = rssi;
    }
  }
}
