using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace virtual_cube
{
    public class CubeBluetoothLEAdvertisementWatcher
    {
        private readonly BluetoothLEAdvertisementWatcher watcher;
        public delegate void DeviceFoundEventHandler(Cube rubiksConnected);
        public event DeviceFoundEventHandler DeviceFoundEvent;
        protected virtual void RaiseDeviceFoundEvent(Cube cube)
        {
            // Raise the event in a thread-safe manner using the ?. operator.
            DeviceFoundEvent?.Invoke(cube);
        }
        public CubeBluetoothLEAdvertisementWatcher()
        {
            watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            watcher.Received += WatcherAdvertisementReceived;
        }

        public void Start()
        {
            watcher.Start();
        }

        public void Stop()
        {
            watcher.Stop();
        }
        private async void WatcherAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.Advertisement.LocalName.StartsWith("R"))
            {
                foreach (BluetoothLEManufacturerData data in args.Advertisement.ManufacturerData)
                {
                    Debug.WriteLine("Company" + data.CompanyId);
                }
                foreach (var data in args.Advertisement.ServiceUuids)
                {
                    Debug.WriteLine("Service:" + data);
                }
                Debug.WriteLine($"Found {args.Advertisement.LocalName} | {Cube.MAC802DOT3(args.BluetoothAddress)} | {args.AdvertisementType}");


                    var cube = CubeFactory.CreateCube(args);
                    if (cube != null)
                    {
                        RaiseDeviceFoundEvent(cube);
                    }
            }
        }
    }
}
