using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace RvtToNavisConverter.Services
{
    public class HardwareIdService : IHardwareIdService
    {
        public string GetHardwareId()
        {
            try
            {
                var macAddress = GetMacAddress();
                var cpuId = GetCpuId();
                var volumeSerial = GetVolumeSerial();

                // Combine all hardware identifiers
                var combinedId = $"{macAddress}-{cpuId}-{volumeSerial}";

                // Generate SHA256 hash
                using (var sha256 = SHA256.Create())
                {
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedId));
                    return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 32); // Return first 32 chars
                }
            }
            catch (Exception ex)
            {
                // If any error occurs, generate a fallback ID
                return GenerateFallbackId();
            }
        }

        private string GetMacAddress()
        {
            try
            {
                var macAddress = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault(mac => !string.IsNullOrEmpty(mac) && mac != "000000000000");

                return !string.IsNullOrEmpty(macAddress) ? macAddress : "NOMAC";
            }
            catch
            {
                return "NOMAC";
            }
        }

        private string GetCpuId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                using (var collection = searcher.Get())
                {
                    foreach (var obj in collection)
                    {
                        var cpuId = obj["ProcessorId"]?.ToString();
                        if (!string.IsNullOrEmpty(cpuId))
                            return cpuId;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return "NOCPU";
        }

        private string GetVolumeSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = 'C:'"))
                using (var collection = searcher.Get())
                {
                    foreach (var obj in collection)
                    {
                        var serial = obj["VolumeSerialNumber"]?.ToString();
                        if (!string.IsNullOrEmpty(serial))
                            return serial;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return "NOVOL";
        }

        private string GenerateFallbackId()
        {
            // Generate a consistent ID based on environment variables and machine name
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var osVersion = Environment.OSVersion.ToString();
            
            var fallbackString = $"{machineName}-{userName}-{osVersion}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackString));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 32);
            }
        }
    }
}