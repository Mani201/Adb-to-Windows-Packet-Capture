using Microsoft.Win32;
using System;
using System.Windows;
using System.Threading.Tasks;
using AdbPacketCapture.Services;
using AdbPacketCapture.Models;

namespace AdbPacketCapture
{
    public partial class MainWindow : Window
    {
        private readonly AdbService adbService;
        private readonly CaptureLogger logger;
        private bool isCapturing;

        public MainWindow()
        {
            InitializeComponent();
            adbService = new AdbService();
            logger = new CaptureLogger();
            SavePath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshDeviceList();
        }

        private async void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDeviceList();
        }

        private async Task RefreshDeviceList()
        {
            try
            {
                var devices = await adbService.GetDevices();
                DeviceList.ItemsSource = devices;
                if (devices.Count > 0)
                    DeviceList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing devices: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PCAP files (*.pcap)|*.pcap|All files (*.*)|*.*",
                DefaultExt = "pcap",
                FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.pcap"
            };

            if (dialog.ShowDialog() == true)
            {
                SavePath.Text = dialog.FileName;
            }
        }

        private async void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceList.SelectedItem == null)
            {
                MessageBox.Show("Please select a device", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string deviceId = DeviceList.SelectedItem.ToString() ?? "";
                var options = new CaptureOptions
                {
                    Filter = FilterText.Text
                };

                await adbService.StartCapture(deviceId, options);
                isCapturing = true;
                UpdateUIState(true);
                LogMessage("Capture started...");

                _ = MonitorCaptureSize(deviceId);
            }
            catch (Exception ex)
            {
                LogMessage($"Error starting capture: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StopCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? deviceId = DeviceList.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(deviceId)) return;

                isCapturing = false;
                await adbService.StopCapture(deviceId, SavePath.Text);
                UpdateUIState(false);
                LogMessage("Capture stopped and saved successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping capture: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUIState(bool isCapturing)
        {
            StartButton.IsEnabled = !isCapturing;
            StopButton.IsEnabled = isCapturing;
            DeviceList.IsEnabled = !isCapturing;
            FilterText.IsEnabled = !isCapturing;
        }

        private async Task MonitorCaptureSize(string deviceId)
        {
            while (isCapturing)
            {
                try
                {
                    var size = await adbService.GetCaptureFileSize(deviceId);
                    LogMessage($"Current capture size: {size} bytes");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error monitoring size: {ex.Message}");
                }
                await Task.Delay(2000);
            }
        }

        private void LogMessage(string message)
        {
            logger.Log(message);
            Dispatcher.Invoke(() =>
            {
                LogText.AppendText($"{DateTime.Now:HH:mm:ss}: {message}\n");
                LogText.ScrollToEnd();
            });
        }
    }
}