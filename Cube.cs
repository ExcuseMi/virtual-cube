using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace virtual_cube
{
    public abstract class Cube : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public ulong BluetoothAddress { get; set; }
        public String FormattedBluetoothAddress { get; set; }
        private DateTime _LastAdvertisement;
        public DateTime LastAdvertisement
        {
            get { return _LastAdvertisement; }
            set
            {
                _LastAdvertisement = value;
                OnPropertyChanged("LastAdvertisement");
            }
        }
        private ConnectionStatus _ConnectionStatus;
        public ConnectionStatus ConnectionStatus
        {
            get { return _ConnectionStatus; }
            set
            {
                _ConnectionStatus = value;
                OnPropertyChanged("ConnectionStatus");
            }
        }

        private int? _BatteryLevel;
        public int? BatteryLevel
        {
            get { return _BatteryLevel; }
            set
            {
                _BatteryLevel = value;
                OnPropertyChanged("BatteryLevel");
            }
        }

        public event EventHandler<Move> Moves;
        public event PropertyChangedEventHandler PropertyChanged;

        public Cube(string name, ulong bluetoothAddress)
        {
            Name = name;
            BluetoothAddress = bluetoothAddress;
            FormattedBluetoothAddress = MAC802DOT3(bluetoothAddress);
            ConnectionStatus = ConnectionStatus.DISCONNECTED;
        }

        public abstract String GetTypeName();
        public abstract Task RequestBatteryLevelAsync();
        public abstract Task ConnectAsync();
        public abstract Task DisconnectAsync();

        protected virtual void OnNewMove(Move move)
        {
            EventHandler<Move> handler = Moves;
            if (handler != null)
            {
                handler(this, move);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            return obj is Cube cube &&
                   BluetoothAddress == cube.BluetoothAddress;
        }

        public override int GetHashCode()
        {
            return 963345907 + BluetoothAddress.GetHashCode();
        }

        public static string MAC802DOT3(ulong macAddress)
        {
            return string.Join(":",
                                BitConverter.GetBytes(macAddress).Reverse()
                                .Select(b => b.ToString("X2"))).Substring(6);
        }
    }
}
