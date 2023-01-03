using System;

namespace AppoMobi.Maui.BLE
{
    static class DefaultTrace
  {
    static DefaultTrace()
    {
      Trace.TraceImplementation = Console.WriteLine;
    }
  }
}
