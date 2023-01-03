using System;

namespace AppoMobi.Maui.BLE.EventArgs
{
    public class MtuRequestCallbackEventArgs : System.EventArgs
    {
        public Exception Error { get; }

        public int Mtu { get; }

        public MtuRequestCallbackEventArgs(Exception error, int mtu)
        {
            Error = error;
            Mtu = mtu;
        }
    }
}
