using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace virtual_cube
{
    class RubiksConnectedCube : Cube
    {

        private static String UUID_SUFFIX = "-b5a3-f393-e0a9-e50e24dcca9e";
        public static Guid SERVICE_UUID = Guid.Parse("6e400001" + UUID_SUFFIX);
        private static Guid CHARACTERISTIC_WRITE_UUID = Guid.Parse("6e400002" + UUID_SUFFIX);
        private static Guid CHARACTERISTIC_READ_UUID = Guid.Parse("6e400003" + UUID_SUFFIX);
        private static byte WRITE_BATTERY = 50;
        private static byte WRITE_STATE = 51;

        private int moveCount = 1;

        public GattCharacteristic ReadData { get; set; }
        public GattCharacteristic WriteData { get; set; }
        public BluetoothLEDevice BluetoothLeDevice { get; set; }

        private IReadOnlyList<GattDeviceService> Services;

        public RubiksConnectedCube(string name, ulong bluetoothAddress) : base(name, bluetoothAddress)
        {
            
        }
        public override String GetTypeName()
        {
            if(Name.StartsWith("GoCube"))
            {
                return "GoCube";
            } else if(Name.StartsWith("Rubiks"))
            {
                return "Rubiks Connected";
            }
            return null;
        }

        public override async Task ConnectAsync()
        {
            if (ConnectionStatus.DISCONNECTED == ConnectionStatus)
            {
                ConnectionStatus = ConnectionStatus.CONNECTING;
                BluetoothLeDevice =  await BluetoothLEDevice.FromBluetoothAddressAsync(BluetoothAddress);
                BluetoothLeDevice.ConnectionStatusChanged += ConnectionStatusChanged;
                var gattDeviceServicesResult = await BluetoothLeDevice.GetGattServicesForUuidAsync(SERVICE_UUID);
                Services = gattDeviceServicesResult.Services;
                var foundService = gattDeviceServicesResult.Services.Single(s => s.Uuid == SERVICE_UUID);
                GattCharacteristicsResult gattCharacteristicsResult = await foundService.GetCharacteristicsAsync();
                ReadData = gattCharacteristicsResult.Characteristics.Single(x => x.Uuid == CHARACTERISTIC_READ_UUID);
                Debug.WriteLine("Found Read Char");
                WriteData = gattCharacteristicsResult.Characteristics.Single(x => x.Uuid == CHARACTERISTIC_WRITE_UUID);
                Debug.WriteLine("Found Write Char");

  
                GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
                do
                {
                    try
                    {
                        status = await ReadData.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        Debug.WriteLine("Notify status: " + status);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Error during notify" + e.Message);

                    }
                } while (status != GattCommunicationStatus.Success);

                ReadData.ValueChanged += HandleRealTimeData;

                await WriteValue();
                ConnectionStatus = ConnectionStatus.CONNECTED;
            }
        }

        public override async Task RequestBatteryLevelAsync()
        {
            var writer = new DataWriter();
            writer.WriteBytes(new byte[] { WRITE_BATTERY });
            await WriteData.WriteValueAsync(writer.DetachBuffer());
        }

        public override async Task DisconnectAsync()
        {
            if (ReadData != null)
            {
                ReadData.ValueChanged -= HandleRealTimeData;
                ReadData = null;
            }
            WriteData = null;
            if (Services != null)
            {
                foreach (GattDeviceService service in Services)
                {
                    service.Dispose();
                }
                Services = null;
            }
            if (BluetoothLeDevice != null)
            {
                BluetoothLeDevice.ConnectionStatusChanged -= ConnectionStatusChanged;
                BluetoothLeDevice.Dispose();
                BluetoothLeDevice = null;
                GC.Collect();
            }
            BatteryLevel = null;
            ConnectionStatus = ConnectionStatus.DISCONNECTED;
        }

        private void HandleRealTimeData(GattCharacteristic c, GattValueChangedEventArgs args)
        {
            IBuffer data = args.CharacteristicValue;
            var bytes = new byte[data.Length];
            CryptographicBuffer.CopyToByteArray(data, out bytes);
            ParseData(bytes);
        }

        private static readonly int[] axisPerm = { 5, 2, 0, 3, 1, 4 };

        private void ParseData(byte[] data)
        {
            if (data.Length < 4)
            {
                return;
            }
            if (data[0] != 0x2a ||
                data[data.Length - 2] != 0x0d ||
                data[data.Length - 1] != 0x0a)
            {
                return;
            }
            var msgType = data[2];
            var msgLen = data.Length - 6;

            if (msgType == 1)
            {
                for (var i = 0; i < msgLen; i += 2)
                {
                    var axis = axisPerm[data[3 + i] >> 1];
                    var power = new int[] { 0, 2 }[data[3 + i] & 1];
                    var m = axis * 3 + power;
                    String move = "" + "URFDLB"[axis] + " 2I"[power];
                    Debug.WriteLine("move: " + move);
                    OnNewMove(MoveHelper.FindMove(move.Trim()));
                }
                if (++moveCount > 20)
                {
                    moveCount = 0;
                    WriteValue();
                }
            }
            else if (msgType == 5)
            { // battery level
                BatteryLevel = data[3];

                Debug.WriteLine($"Battery-level: {BatteryLevel}");
            }
        }

        private async Task WriteValue()
        {
            var writer = new DataWriter();
            writer.WriteBytes(new byte[] { WRITE_STATE });
            await WriteData.WriteValueAsync(writer.DetachBuffer());
        }


        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine(args);
        }


    }
}
