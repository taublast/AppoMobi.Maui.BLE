﻿using Android.Bluetooth;
using AppoMobi.Maui.BLE.Enums;

namespace AppoMobi.Maui.BLE.Extensions
{
    public static class BluetoothStateExtension
    {
        public static BluetoothState ToBluetoothState(this State state)
        {
            switch (state)
            {
                case State.Connected:
                case State.Connecting:
                case State.Disconnected:
                case State.Disconnecting:
                    return BluetoothState.On;

                case State.Off:
                    return BluetoothState.Off;

                case State.On:
                    return BluetoothState.On;

                case State.TurningOff:
                    return BluetoothState.TurningOff;

                case State.TurningOn:
                    return BluetoothState.TurningOn;

                default:
                    return BluetoothState.Unknown;
            }
        }
    }
}
