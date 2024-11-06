using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AdbPacketCapture.Models;


namespace AdbPacketCapture.Services
{

    public class AdbService
    {
        string captureDir = "/storage/my_capture_Data";
        private Process? currentCaptureProcess;

        public async Task<List<string>> GetDevices()
        {
            var devices = new List<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "devices",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            foreach (string line in output.Split('\n'))
            {
                if (line.Contains("\tdevice"))
                {
                    devices.Add(line.Split('\t')[0]);
                }
            }

            return devices;
        }

        public async Task StartCapture(string deviceId, CaptureOptions options)
        {
            string command = options.BuildTcpdumpCommand();
            string adbCommand = $"shell {command}";

            currentCaptureProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {deviceId} {adbCommand}",
                    //Arguments = $"-s c3ed44535eb7d217 shell tcpdump -i any -w /storage/my_capture_Data/captrue.pcap",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
             // 표준 출력 이벤트 핸들러
            currentCaptureProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"Output: {e.Data}");
            };

            // 에러 출력 이벤트 핸들러
            currentCaptureProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"Error: {e.Data}");
            };

            currentCaptureProcess.Start();
            currentCaptureProcess.BeginOutputReadLine();
            currentCaptureProcess.BeginErrorReadLine();
        }

        public async Task StopCapture(string deviceId, string savePath)
        {


            if (currentCaptureProcess != null && !currentCaptureProcess.HasExited)
            {
                currentCaptureProcess.Kill();
                await currentCaptureProcess.WaitForExitAsync();
            }

            var pullProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {deviceId} pull {captureDir}/capture.pcap \"{savePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            pullProcess.Start();
            await pullProcess.WaitForExitAsync();

            await ExecuteAdbCommand(deviceId, $"shell rm {captureDir}/capture.pcap");
        }

        public async Task<long> GetCaptureFileSize(string deviceId)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {deviceId} shell ls -l {captureDir}/capture.pcap",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var parts = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 5 && long.TryParse(parts[4], out long size))
            {
                return size;
            }
            return 0;
        }

        private async Task ExecuteAdbCommand(string deviceId, string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {deviceId} {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
    }
}