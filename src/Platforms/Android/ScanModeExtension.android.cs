﻿using System;
using Android.OS;

using AppoMobi.Maui.BLE.Enums;
using AndroidScanMode = Android.Bluetooth.LE.ScanMode;
using Trace = AppoMobi.Maui.BLE.Trace;

namespace AppoMobi.Maui.BLE.Extensions
{
    /// <summary>
    /// See https://developer.android.com/reference/android/bluetooth/le/ScanSettings.html
    /// </summary>
    internal static class ScanModeExtension
    {
        public static AndroidScanMode ToNative(this ScanMode scanMode)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
                throw new InvalidOperationException("Scan modes are not implemented in API lvl < 21.");

            switch (scanMode)
            {
                case ScanMode.Passive:
                    if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                    {
                        Trace.WriteLine("Scanmode Passive is not supported on API lvl < 23. Falling back to LowPower.");
                        return AndroidScanMode.LowPower;
                    }
                    return AndroidScanMode.Opportunistic;
                case ScanMode.LowPower:
                    return AndroidScanMode.LowPower;
                case ScanMode.Balanced:
                    return AndroidScanMode.Balanced;
                case ScanMode.LowLatency:
                    return AndroidScanMode.LowLatency;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scanMode), scanMode, null);
            }
        }
    }
}
