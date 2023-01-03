namespace AppoMobi.Maui.BLE
{
    public static class AppHostBuilderExtensions
    {
        public static MauiAppBuilder UseBlootoothLE(this MauiAppBuilder builder)
        {

            builder.Services.AddSingleton<IBluetoothLE, BluetoothLE>();

            return builder;
        }
    }
}
