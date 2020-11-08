using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace virtual_cube
{
    class CubeFactory
    {
        public static Cube CreateCube(BluetoothLEAdvertisementReceivedEventArgs bluetoothLEAdvertisementReceived) {

            //if(bluetoothLEAdvertisementReceived.Advertisement.ServiceUuids.Contains(RubiksConnectedCube.SERVICE_UUID))
            if(bluetoothLEAdvertisementReceived.Advertisement.LocalName.StartsWith("Rubiks") ||
                bluetoothLEAdvertisementReceived.Advertisement.LocalName.StartsWith("GoCube"))
            {
                Debug.WriteLine($"Adding {bluetoothLEAdvertisementReceived.Advertisement.LocalName} | {Cube.MAC802DOT3(bluetoothLEAdvertisementReceived.BluetoothAddress)} {bluetoothLEAdvertisementReceived.AdvertisementType}");

                return new RubiksConnectedCube(
                    bluetoothLEAdvertisementReceived.Advertisement.LocalName,
                    bluetoothLEAdvertisementReceived.BluetoothAddress);
            }
            return null;
        }  
    }
}
