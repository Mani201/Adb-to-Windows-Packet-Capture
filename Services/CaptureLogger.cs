using System;
using System.IO;

namespace AdbPacketCapture.Services
{
    public class CaptureLogger
    {
        private readonly string logFile = "capture_log.txt";

        public void Log(string message)
        {
            try
            {
                File.AppendAllText(logFile, $"{DateTime.Now}: {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}