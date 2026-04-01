using System;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Drawing;

// BLE
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLE_SERIAL_TERMINAL
{
    public partial class TerminalScreen : Form
    {
        // ================= UUID =================
        private static readonly Guid SERVICE_UUID = Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        private static readonly Guid TX_UUID = Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        private static readonly Guid RX_UUID = Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e");

        // ================= VARIABLES =================
        private BluetoothLEAdvertisementWatcher watcher;
        private BluetoothLEDevice bleDevice;
        private GattCharacteristic txCharacteristic;
        private GattCharacteristic rxCharacteristic;
        private HashSet<ulong> discoveredDevices = new HashSet<ulong>();

        private SerialPort serialPort;
        private BleDeviceItem selectedDevice;
        private bool isScanning = false;
        private bool isConnected = false;

        // 🔥 NEW: Scan Animation
        private System.Windows.Forms.Timer scanTimer; private int dotCount = 0;

        public TerminalScreen()
        {
            InitializeComponent();

            btnScan.Click += btnScan_Click;
            btnSend.Click += btnSend_Click;
            btnConnect.Click += btnConnect_Click;
            btnClearLog.Click += btnClearLog_Click;
            btnSaveLog.Click += btnSaveLog_Click;

            // 🔥 Init scan animation
            scanTimer = new System.Windows.Forms.Timer();
            scanTimer.Interval = 400;
            scanTimer.Tick += ScanTimer_Tick;

            UpdateConnectionUI(false); // initial state
        }

        // ================= UI HELPERS =================
        private void UpdateConnectionUI(bool connected)
        {
            isConnected = connected;

            if (connected)
            {
                btnConnect.Text = "Disconnect";
                btnConnect.FillColor = Color.FromArgb(0, 90, 160);

                lblStatus.Text = "● Connected";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                btnConnect.Text = "Connect";
                btnConnect.FillColor = Color.FromArgb(0, 122, 204);

                lblStatus.Text = "● Disconnected";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            dotCount++;
            if (dotCount > 3) dotCount = 1;

            btnScan.Text = "Scanning" + new string('.', dotCount);
        }

        private void LoadClassicDevices()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
                {
                    foreach (var device in searcher.Get())
                    {
                        string name = device["Name"]?.ToString();

                        if (string.IsNullOrEmpty(name)) continue;

                        int start = name.LastIndexOf("(COM");
                        int end = name.LastIndexOf(")");

                        if (start == -1 || end == -1) continue;

                        string comPort = name.Substring(start + 1, end - start - 1);

                        ddlDevices.Items.Add(new BleDeviceItem
                        {
                            Name = name.Replace($"({comPort})", "").Trim(),
                            ComPort = comPort,
                            IsBle = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Serial load error: " + ex.Message);
            }
        }

        private void Rx_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);

            string raw = Encoding.UTF8.GetString(data);

            this.Invoke(() => Log(raw));
        }

        private async System.Threading.Tasks.Task EnableNotifications()
        {
            if (rxCharacteristic == null) { Log("RX Characteristic not found "); return; }
            rxCharacteristic.ValueChanged -= Rx_ValueChanged; // prevent duplicate
            rxCharacteristic.ValueChanged += Rx_ValueChanged;
            var status = await rxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync( GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status == GattCommunicationStatus.Success) { Log("Notifications enabled "); }
            else { Log("Failed to enable notifications "); } 
        }

        // ================= MODEL =================
        public class BleDeviceItem
        {
            public string Name { get; set; }
            public ulong Address { get; set; }
            public bool IsBle { get; set; }
            public string ComPort { get; set; }

            public override string ToString()
            {
                return IsBle
                    ? $"{Name} ({Address:X})"
                    : $"{Name} ({ComPort})";
            }
        }

        // ================= SCAN =================
        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (!isScanning)
            {
                ddlDevices.Items.Clear();
                discoveredDevices.Clear();

                StartBleScan();
                LoadClassicDevices();

                isScanning = true;
                scanTimer.Start(); // 🔥 animation

                Log("Scanning started...");

                await System.Threading.Tasks.Task.Delay(12000);

                if (isScanning)
                    StopScan();
            }
            else
            {
                StopScan();
            }
        }

        private void StopScan()
        {
            watcher?.Stop();
            scanTimer.Stop(); // 🔥 stop animation

            isScanning = false;
            btnScan.Text = "Scan Nearby Devices";

            Log("Scan stopped");
        }

        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.RawSignalStrengthInDBm < -75)
                return;

            string name = args.Advertisement.LocalName;
            bool hasService = args.Advertisement.ServiceUuids.Count > 0;

            if (string.IsNullOrEmpty(name) && !hasService)
                return;

            if (discoveredDevices.Contains(args.BluetoothAddress))
                return;

            discoveredDevices.Add(args.BluetoothAddress);

            if (string.IsNullOrEmpty(name))
                name = "BLE Device";

            this.Invoke(() =>
            {
                ddlDevices.Items.Add(new BleDeviceItem
                {
                    Name = $"{name}",
                    Address = args.BluetoothAddress,
                    IsBle = true
                });
            });
        }

        private void StartBleScan()
        {
            watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            watcher.Received -= Watcher_Received;
            watcher.Received += Watcher_Received;
            watcher.Start();
        }

        // ================= DISCONNECT =================
        //private async void DisconnectAll()
        //{
        //    try
        //    {
        //        watcher?.Stop();

        //        if (rxCharacteristic != null)
        //        {
        //            rxCharacteristic.ValueChanged -= Rx_ValueChanged;
        //            await rxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
        //                GattClientCharacteristicConfigurationDescriptorValue.None);
        //        }

        //        bleDevice?.Dispose();
        //        bleDevice = null;

        //        txCharacteristic = null;
        //        rxCharacteristic = null;

        //        if (serialPort != null && serialPort.IsOpen)
        //        {
        //            serialPort.Close();
        //            serialPort.Dispose();
        //            serialPort = null;
        //        }

        //        UpdateConnectionUI(false);

        //        Log("Disconnected");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log("Disconnect Error: " + ex.Message);
        //    }
        //}
        private async void DisconnectAll()
        {
            try
            {
                Log("Initiating disconnect...");

                // 1. Stop the scanner if it's running
                watcher?.Stop();

                // 2. Cleanup BLE Characteristics
                if (rxCharacteristic != null)
                {
                    try
                    {
                        // Unsubscribe from hardware events
                        rxCharacteristic.ValueChanged -= Rx_ValueChanged;

                        // Tell the device to stop notifying
                        await rxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.None);
                    }
                    catch { /* Device might be out of range already */ }
                }

                // 3. THE FORCE BREAK: Close the GATT Session explicitly
                if (bleDevice != null)
                {
                    try
                    {
                        // We create a session handle specifically to tell Windows to drop the link
                        using (var session = await GattSession.FromDeviceIdAsync(bleDevice.BluetoothDeviceId))
                        {
                            session.MaintainConnection = false;
                            // Disposing the session here (via 'using') triggers the hardware hang-up
                        }
                    }
                    catch { /* Session might already be closed */ }

                    bleDevice.Dispose();
                }

                // 4. Cleanup Serial Port (Classic Bluetooth/COM)
                if (serialPort != null)
                {
                    if (serialPort.IsOpen) serialPort.Close();
                    serialPort.Dispose();
                    serialPort = null;
                }

                // 5. Reset UI and Variables
                bleDevice = null;
                txCharacteristic = null;
                rxCharacteristic = null;
                isConnected = false;

                // 6. Force Garbage Collection 
                // This ensures Windows releases the BLE radio handles immediately
                GC.Collect();
                GC.WaitForPendingFinalizers();

                UpdateConnectionUI(false);
                Log("Disconnected successfully.");
            }
            catch (Exception ex)
            {
                Log("Disconnect Error: " + ex.Message);
            }
        }

        // ================= CONNECT =================
        private async void ConnectBle(ulong address)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(500);

                txCharacteristic = null;
                rxCharacteristic = null;

                bleDevice?.Dispose();
                bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

                if (bleDevice == null)
                {
                    Log("Failed to connect device");
                    return;
                }

                var services = await bleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                foreach (var service in services.Services)
                {
                    if (service.Uuid == SERVICE_UUID)
                    {
                        var chars = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

                        foreach (var ch in chars.Characteristics)
                        {
                            if (ch.Uuid == TX_UUID) txCharacteristic = ch;
                            if (ch.Uuid == RX_UUID) rxCharacteristic = ch;
                        }
                    }
                }

                if (txCharacteristic == null || rxCharacteristic == null)
                {
                    Log("Required characteristics not found");
                    return;
                }

                await EnableNotifications();

                UpdateConnectionUI(true);

                Log("BLE Connected");
            }
            catch (Exception ex)
            {
                Log("BLE Error: " + ex.Message);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                selectedDevice = ddlDevices.SelectedItem as BleDeviceItem;

                if (selectedDevice == null)
                {
                    Log("Select device first");
                    return;
                }

                if (selectedDevice.IsBle)
                    ConnectBle(selectedDevice.Address);
                else
                    ConnectSerial();
            }
            else
            {
                DisconnectAll();
            }
        }

        // ================= SERIAL =================
        private void ConnectSerial()
        {
            try
            {
                var device = ddlDevices.SelectedItem as BleDeviceItem;

                if (device == null || string.IsNullOrEmpty(device.ComPort))
                {
                    Log("Invalid COM port ❌");
                    return;
                }

                serialPort = new SerialPort(device.ComPort, 9600);

                serialPort.DataReceived += (s, e) =>
                {
                    string data = serialPort.ReadExisting();
                    this.Invoke(() => Log(data));
                };

                serialPort.Open();

                UpdateConnectionUI(true);

                Log($"Serial Connected ({device.ComPort})");
            }
            catch (Exception ex)
            {
                Log("Serial Error: " + ex.Message);
            }
        }

        // ================= SEND =================
        private async void btnSend_Click(object sender, EventArgs e)
        {
            string text = txtSend.Text;

            if (string.IsNullOrEmpty(text)) return;

            try
            {
                if (selectedDevice?.IsBle == true)
                {
                    if (txCharacteristic == null)
                    {
                        Log("TX Characteristic not found");
                        return;
                    }

                    var writer = new DataWriter();
                    writer.WriteString(text);

                    await txCharacteristic.WriteValueAsync(writer.DetachBuffer());
                }
                else
                {
                    serialPort?.WriteLine(text);
                }

                if (chkEcho.Checked)
                    Log("TX: " + text);
            }
            catch (Exception ex)
            {
                Log("Send Error: " + ex.Message);
            }
        }

        // ================= LOG =================
        private void Log(string message)
        {
            if (chkTimestamp.Checked)
                message = $"{DateTime.Now:HH:mm:ss} | {message}";

            txtLogs.AppendText(message + Environment.NewLine);
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLogs.Clear();
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtLogs.Text))
                {
                    Log("Nothing to save");
                    return;
                }

                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Text Files (*.txt)|*.txt";
                    saveDialog.FileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        System.IO.File.WriteAllText(saveDialog.FileName, txtLogs.Text);
                        Log("Log saved successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Save Error: " + ex.Message);
            }
        }
    }
}