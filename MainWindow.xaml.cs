using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

using System.Windows;
using System.Windows.Controls;

using WindowsInput.Native;
using System.Reflection;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsInput;
using System.Text.Json.Serialization;
using System.Windows.Media;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;
using ControlzEx.Theming;


namespace virtual_cube
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static readonly int DEFAULT_KEYPRESS_TIME = 20;
        private static readonly CubeBluetoothLEAdvertisementWatcher watcher = new CubeBluetoothLEAdvertisementWatcher();
        public static Boolean mappingEnabled = false;
        public List<VirtualKeyCode> VIRTUAL_KEY_CODES;
        public static Boolean Active { get; set; }
        public ScanMode ScanMode { get; set; }
        private readonly static String path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private readonly static String configurationPath = path + "\\config.json";
        private Boolean loading = true;
        private BindingList<Cube> Cubes = new BindingList<Cube>();
        private Timer batteryTimer;
        private Configuration Configuration = new Configuration();
        private static readonly InputSimulator InputSimulator = new InputSimulator();
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters ={
                        new JsonStringEnumConverter(),
                    }
        };
        public MainWindow()
        {
            InitializeComponent();
            ToggleKeyMapping();
            Active = true;
            watcher.DeviceFoundEvent += DeviceFound;
            StartScan();
            VIRTUAL_KEY_CODES = ((VirtualKeyCode[])Enum.GetValues(typeof(VirtualKeyCode))).ToList();

            F.ItemsSource = VIRTUAL_KEY_CODES;
            FI.ItemsSource = VIRTUAL_KEY_CODES;
            L.ItemsSource = VIRTUAL_KEY_CODES;
            LI.ItemsSource = VIRTUAL_KEY_CODES;
            R.ItemsSource = VIRTUAL_KEY_CODES;
            RI.ItemsSource = VIRTUAL_KEY_CODES;
            U.ItemsSource = VIRTUAL_KEY_CODES;
            UI.ItemsSource = VIRTUAL_KEY_CODES;
            B.ItemsSource = VIRTUAL_KEY_CODES;
            BI.ItemsSource = VIRTUAL_KEY_CODES;
            D.ItemsSource = VIRTUAL_KEY_CODES;
            DI.ItemsSource = VIRTUAL_KEY_CODES;

            Configuration = LoadConfiguration();
            if (Configuration != null && Configuration.Mapping != null) {
                foreach (UIElement uIElement in KeyMappingGrid.Children)
                {
                    if (uIElement is System.Windows.Controls.ComboBox)
                    {
                        System.Windows.Controls.ComboBox comboBox = (System.Windows.Controls.ComboBox)uIElement;
                        VirtualKeyCode mapping;
                        Configuration.Mapping.TryGetValue(comboBox.Name, out mapping);
                        comboBox.SelectedValue = mapping;
                    }
                }
                keyMapping.IsChecked = Configuration.MappingEnabled;
                PRESSTIME.Text = Configuration.TimePerKeyPress?.ToString();
            }
            loading = false;
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);
            batteryTimer = new System.Threading.Timer((e) =>
            {
                foreach(Cube cube in Cubes.Where(x => x.ConnectionStatus == ConnectionStatus.CONNECTED))
                {
                    cube.RequestBatteryLevelAsync();
                }
            }, null, startTimeSpan, periodTimeSpan);
        }

        private void StartScan()
        {
            watcher.Start();
            ScanMode = ScanMode.SCANNING;
            ScanButton.Content = "Stop Scanning";
        }

        private void StopScan()
        {
            watcher.Stop();
            ScanMode = ScanMode.NONE;
            ScanButton.Content = "Start Scanning";
        }

        protected override void OnActivated(EventArgs e)
        {
            Active = true;
        }


        protected override void OnDeactivated(EventArgs e)
        {
            Active = false;
        }

        private void HandleCheck(object sender, RoutedEventArgs e)
        {
            ToggleKeyMapping();

        }

        private void ToggleKeyMapping()
        {
            mappingEnabled = keyMapping != null && keyMapping.IsChecked != null && keyMapping.IsChecked == true;
            if (mappingEnabled)
            {
                UpdateMapping(null, null);
            }
            if (KeyMappingGrid != null)
            {
                foreach (UIElement uIElement in KeyMappingGrid.Children)
                {
                    uIElement.IsEnabled = mappingEnabled;
                }
            }
        }
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            Cube cube = ((Cube)fe.DataContext);
            Disconnect(cube);
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            Cube cube = ((Cube)fe.DataContext);
            Connect(cube);
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScanMode.NONE == ScanMode)
            {
                StartScan();
            } else
            {
                StopScan();
            }
        }


        private async void Connect(Cube cube)
        {
            await cube.ConnectAsync();
            cube.Moves += OnMove;
            StopScan();

            await cube.RequestBatteryLevelAsync();
        }


        private async void Disconnect(Cube cube)
        {
            await cube.DisconnectAsync();
            cube.Moves -= OnMove;
            Application.Current.Dispatcher.Invoke(new Action(() => {
                    Cubes.Remove(cube);
                    cubeGrid.ItemsSource = Cubes;
            }));
            StartScan();
        }

        private void OnMove(object sender, Move move)
        {
            if (!Active)
            {
                if (mappingEnabled)
                {
                    VirtualKeyCode map;
                    Configuration.Mapping.TryGetValue(move.ToString(), out map);
                    if (Configuration.Mapping != null)
                    {
                        Debug.WriteLine("Typing " + map);
                        InputSimulator.Keyboard.KeyDown(map);
                        SendUpWithDelay(map);
                    }
                }
                else
                {
                    InputSimulator.Keyboard.TextEntry(move.ToString().Replace("I","'"));
                }
            }
            else
            {
                HighLight(move);
            }
        }

        async Task SendUpWithDelay(VirtualKeyCode a)
        {
            int delay = Configuration.TimePerKeyPress ?? DEFAULT_KEYPRESS_TIME;
            await Task.Delay(delay);
            InputSimulator.Keyboard.KeyUp(a);
        }

        async Task HighLight(Move move)
        {
            SetLabelStyle(move, Brushes.LightBlue, Brushes.White);
            int delay = Configuration.TimePerKeyPress ?? DEFAULT_KEYPRESS_TIME;

            await Task.Delay(delay);
            SetLabelStyle(move, null, Brushes.Black);
        }

        private void SetLabelStyle(Move move, Brush background, Brush foreGround)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                foreach (UIElement uIElement in KeyMappingGrid.Children)
                {
                    if (uIElement is System.Windows.Controls.Label)
                    {
                        System.Windows.Controls.Label label = (System.Windows.Controls.Label)uIElement;
                        if (label.Name == "LABEL_" + move.ToString())
                        {
                            label.Background = background;
                            label.Foreground = foreGround;
                        }
                    }
                }
            
            }));


        }

        void DeviceFound(Cube cube)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                if (!Cubes.Contains(cube))
                {
                    Cubes.Add(cube);
                    cubeGrid.ItemsSource = Cubes;
                }
            }));
        }

        public void UpdateMapping(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfiguration();
        }

        public void UpdateConfiguration()
        {
            if (!loading)
            {

                Configuration = new Configuration(mappingEnabled, GetMapping(), GetPressTime());
                SaveConfiguration(Configuration);
            }
        }

        private int GetPressTime()
        {
            int number;

            bool success = Int32.TryParse(PRESSTIME.Text, out number);
            if (!success)
            {
                number = DEFAULT_KEYPRESS_TIME;
            }

            return number;
        }

        public Dictionary<String, VirtualKeyCode> GetMapping()
        {
            Dictionary<String, VirtualKeyCode> mapping = new Dictionary<String, VirtualKeyCode>();
            foreach (UIElement uIElement in KeyMappingGrid.Children) {
                if (uIElement is System.Windows.Controls.ComboBox)
                {
                    System.Windows.Controls.ComboBox comboBox = (System.Windows.Controls.ComboBox)uIElement;
                    if (comboBox.SelectedValue != null)
                    {
                        mapping.Add(comboBox.Name, (VirtualKeyCode)comboBox.SelectedValue);
                    }
                }
            }
            return mapping;
        }

        private async void SaveConfiguration(Configuration configuration)
        {
                 using (FileStream fs = File.Create(configurationPath))
                {

                    await JsonSerializer.SerializeAsync(fs, configuration, options);
                }
        }

        private void Refresh()
        {

        }

        private Configuration LoadConfiguration()
        {
            if (File.Exists(configurationPath))
            {
                try
                {
                    return JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configurationPath), options);
                } catch(Exception e) { }

            }
            return null;
        }
        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }



        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void PRESSTIME_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateConfiguration();
        }
    }
}
