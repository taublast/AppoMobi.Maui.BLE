using AppoMobi.Maui.BLE.Utils;
using CoreBluetooth;

namespace AppoMobi.Maui.BLE
{
    public partial class Service
    {
        private readonly CBPeripheral _device;
        private readonly IBleCentralManagerDelegate _bleCentralManagerDelegate;

        internal Guid NativeGuid => NativeService.UUID.GuidFromUuid();

        internal bool NativeIsPrimary => NativeService.Primary;

        internal CBService NativeService { get; private set; }

        internal Service(CBService nativeService, Maui.BLE.Device device, IBleCentralManagerDelegate bleCentralManagerDelegate)
          : this(device)
        {
            NativeService = nativeService;

            _device = device.NativeDevice as CBPeripheral;
            _bleCentralManagerDelegate = bleCentralManagerDelegate;
        }

        internal Task<IList<Maui.BLE.Characteristic>> GetCharacteristicsNativeAsync()
        {
            var exception = new Exception($"Device '{Device.Id}' disconnected while fetching characteristics for service with {Id}.");

            return TaskBuilder.FromEvent<IList<Maui.BLE.Characteristic>, EventHandler<CBServiceEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
              execute: () =>
              {
                  if (_device.State != CBPeripheralState.Connected)
                      throw exception;

                  _device.DiscoverCharacteristics(NativeService);
              },
              getCompleteHandler: (complete, reject) => (sender, args) =>
              {
                  if (args.Error != null)
                  {
                      reject(new Exception($"Discover characteristics error: {args.Error.Description}"));
                  }
                  else
                  if (args.Service?.Characteristics == null)
                  {
                      reject(new Exception($"Discover characteristics error: returned list is null"));
                  }
                  else
                  {
                      var characteristics = args.Service.Characteristics
                                                .Select(characteristic => new Maui.BLE.Characteristic(characteristic, _device, this, _bleCentralManagerDelegate))
                                                .Cast<Maui.BLE.Characteristic>().ToList();
                      complete(characteristics);
                  }
              },
              subscribeComplete: handler => _device.DiscoveredCharacteristics += handler,
              unsubscribeComplete: handler => _device.DiscoveredCharacteristics -= handler,
              getRejectHandler: reject => ((sender, args) =>
              {
                  if (args.Peripheral.Identifier == _device.Identifier)
                      reject(exception);
              }),
              subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
              unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
        }

        public virtual void Dispose()
        {
        }
    }
}
