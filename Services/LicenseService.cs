using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;
using RvtToNavisConverter.Models;

namespace RvtToNavisConverter.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly IHardwareIdService _hardwareIdService;
        private readonly ICryptoService _cryptoService;
        private readonly IRsaCryptoService _rsaCryptoService;
        private readonly string _licenseFilePath;
        private readonly string _registryKey = @"SOFTWARE\RvtToNavisConverter";
        private readonly string _registryValue = "LicenseData";

        public LicenseService(IHardwareIdService hardwareIdService, ICryptoService cryptoService)
        {
            _hardwareIdService = hardwareIdService;
            _cryptoService = cryptoService;
            _rsaCryptoService = new RsaCryptoService();
            
            // Store license in AppData\Local
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDirectory = Path.Combine(appDataPath, "RvtToNavisConverter");
            Directory.CreateDirectory(appDirectory);
            _licenseFilePath = Path.Combine(appDirectory, ".license");
            
            // Hide the license file
            if (File.Exists(_licenseFilePath))
            {
                File.SetAttributes(_licenseFilePath, FileAttributes.Hidden | FileAttributes.System);
            }
        }

        public LicenseValidationResult ValidateLicense()
        {
            try
            {
                var license = LoadLicense();
                if (license == null)
                {
                    return new LicenseValidationResult
                    {
                        Status = LicenseStatus.NotFound,
                        Message = "No license found. Creating trial license.",
                        DaysRemaining = 0
                    };
                }

                // Verify hardware ID
                var currentHardwareId = _hardwareIdService.GetHardwareId();
                if (license.HardwareId != currentHardwareId)
                {
                    return new LicenseValidationResult
                    {
                        Status = LicenseStatus.Invalid,
                        Message = "License is not valid for this machine.",
                        DaysRemaining = 0,
                        HardwareId = currentHardwareId
                    };
                }

                // Verify license integrity
                if (!string.IsNullOrEmpty(license.Signature))
                {
                    // New RSA-based license
                    var licenseData = $"{license.HardwareId}|{license.TrialStartDate:yyyy-MM-dd}|{license.TrialDays}|{license.LicenseType}|{license.CustomerName}|{license.CustomerEmail}";
                    if (!_rsaCryptoService.VerifySignature(licenseData, license.Signature, null))
                    {
                        return new LicenseValidationResult
                        {
                            Status = LicenseStatus.Tampered,
                            Message = "License signature verification failed.",
                            DaysRemaining = 0
                        };
                    }
                }
                else if (!string.IsNullOrEmpty(license.Checksum))
                {
                    // Legacy checksum-based license
                    var licenseData = $"{license.HardwareId}|{license.TrialStartDate:O}|{license.TrialDays}|{license.LicenseType}";
                    if (!_cryptoService.VerifyChecksum(licenseData, license.Checksum))
                    {
                        return new LicenseValidationResult
                        {
                            Status = LicenseStatus.Tampered,
                            Message = "License file has been tampered with.",
                            DaysRemaining = 0
                        };
                    }
                }
                else
                {
                    return new LicenseValidationResult
                    {
                        Status = LicenseStatus.Invalid,
                        Message = "Invalid license format.",
                        DaysRemaining = 0
                    };
                }

                // Check trial period
                var daysSinceStart = (DateTime.Now - license.TrialStartDate).Days;
                var daysRemaining = license.TrialDays - daysSinceStart;

                // Check for system clock manipulation
                if (daysSinceStart < 0)
                {
                    return new LicenseValidationResult
                    {
                        Status = LicenseStatus.Tampered,
                        Message = "System clock manipulation detected.",
                        DaysRemaining = 0
                    };
                }

                if (daysRemaining <= 0)
                {
                    return new LicenseValidationResult
                    {
                        Status = LicenseStatus.Expired,
                        Message = "Trial period has expired.",
                        DaysRemaining = 0,
                        HardwareId = currentHardwareId
                    };
                }

                return new LicenseValidationResult
                {
                    Status = LicenseStatus.Valid,
                    Message = $"Trial license valid. {daysRemaining} days remaining.",
                    DaysRemaining = daysRemaining,
                    HardwareId = currentHardwareId
                };
            }
            catch (Exception ex)
            {
                return new LicenseValidationResult
                {
                    Status = LicenseStatus.Invalid,
                    Message = $"Error validating license: {ex.Message}",
                    DaysRemaining = 0
                };
            }
        }

        public LicenseValidationResult CreateTrialLicense()
        {
            try
            {
                var hardwareId = _hardwareIdService.GetHardwareId();
                var license = new License
                {
                    HardwareId = hardwareId,
                    TrialStartDate = DateTime.Now,
                    TrialDays = 30,
                    LicenseType = "Trial"
                };

                // For trial licenses created by the app, still use checksum (no private key available)
                var licenseData = $"{license.HardwareId}|{license.TrialStartDate:O}|{license.TrialDays}|{license.LicenseType}";
                license.Checksum = _cryptoService.ComputeChecksum(licenseData);
                license.CustomerName = "Trial User";
                license.CustomerEmail = "trial@local";

                SaveLicense(license);

                return new LicenseValidationResult
                {
                    Status = LicenseStatus.Valid,
                    Message = "Trial license created successfully.",
                    DaysRemaining = 30,
                    HardwareId = hardwareId
                };
            }
            catch (Exception ex)
            {
                return new LicenseValidationResult
                {
                    Status = LicenseStatus.Invalid,
                    Message = $"Error creating trial license: {ex.Message}",
                    DaysRemaining = 0
                };
            }
        }

        public void SaveLicense(License license)
        {
            try
            {
                var json = JsonConvert.SerializeObject(license, Formatting.Indented);
                var encryptedData = _cryptoService.Encrypt(json);
                
                // Save to file
                File.WriteAllText(_licenseFilePath, encryptedData);
                File.SetAttributes(_licenseFilePath, FileAttributes.Hidden | FileAttributes.System);
                
                // Save backup to registry
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(_registryKey))
                    {
                        key?.SetValue(_registryValue, encryptedData);
                    }
                }
                catch
                {
                    // Registry backup is optional, continue if it fails
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save license: {ex.Message}", ex);
            }
        }

        public License LoadLicense()
        {
            try
            {
                string encryptedData = null;

                // Try to load from file first
                if (File.Exists(_licenseFilePath))
                {
                    encryptedData = File.ReadAllText(_licenseFilePath);
                }
                // If file doesn't exist, try registry
                else
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(_registryKey))
                        {
                            encryptedData = key?.GetValue(_registryValue) as string;
                        }
                        
                        // If found in registry, restore to file
                        if (!string.IsNullOrEmpty(encryptedData))
                        {
                            File.WriteAllText(_licenseFilePath, encryptedData);
                            File.SetAttributes(_licenseFilePath, FileAttributes.Hidden | FileAttributes.System);
                        }
                    }
                    catch
                    {
                        // Registry read failed, continue
                    }
                }

                if (string.IsNullOrEmpty(encryptedData))
                    return null;

                string json;
                
                // Try to decrypt with license generator key first (for customer licenses)
                try
                {
                    json = DecryptLicenseGeneratorFile(encryptedData);
                }
                catch
                {
                    // Fall back to local encryption (for trial licenses)
                    json = _cryptoService.Decrypt(encryptedData);
                }
                
                if (string.IsNullOrEmpty(json))
                    return null;

                return JsonConvert.DeserializeObject<License>(json);
            }
            catch
            {
                return null;
            }
        }

        private string DecryptLicenseGeneratorFile(string encryptedData)
        {
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                var key = System.Text.Encoding.UTF8.GetBytes("LicGen2025BTnkut!@#$%^&*()123456");
                var iv = System.Text.Encoding.UTF8.GetBytes("1234567890123456");

                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var encryptedBytes = Convert.FromBase64String(encryptedData);
                    var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return System.Text.Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        public void ClearLicense()
        {
            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    File.Delete(_licenseFilePath);
                }

                using (var key = Registry.CurrentUser.OpenSubKey(_registryKey, true))
                {
                    key?.DeleteValue(_registryValue, false);
                }
            }
            catch
            {
                // Ignore errors when clearing
            }
        }

        public bool CheckAndImportLicenseFile()
        {
            try
            {
                // Check for .lic files in application directory
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var licenseFiles = Directory.GetFiles(appDirectory, "*.lic");

                // Also check in %AppData%\RvtToNavisConverter\
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RvtToNavisConverter");
                if (Directory.Exists(appDataPath))
                {
                    var appDataLicenseFiles = Directory.GetFiles(appDataPath, "*.lic");
                    licenseFiles = licenseFiles.Concat(appDataLicenseFiles).ToArray();
                }

                if (licenseFiles.Length == 0)
                    return false;

                // Try to import the most recent license file
                var latestLicenseFile = licenseFiles.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();
                
                try
                {
                    // Read and decrypt the license file
                    var encryptedData = File.ReadAllText(latestLicenseFile);
                    var json = DecryptLicenseGeneratorFile(encryptedData);
                    var license = JsonConvert.DeserializeObject<License>(json);

                    // Verify the license is for this machine
                    var currentHardwareId = _hardwareIdService.GetHardwareId();
                    if (license.HardwareId != currentHardwareId)
                    {
                        // Wrong hardware ID, don't import
                        return false;
                    }

                    // Verify RSA signature
                    if (!string.IsNullOrEmpty(license.Signature))
                    {
                        if (!_rsaCryptoService.VerifyLicenseSignature(license))
                        {
                            // Invalid signature, don't import
                            return false;
                        }
                    }

                    // Save the license to the standard location
                    SaveLicense(license);

                    // Delete the imported file
                    File.Delete(latestLicenseFile);

                    return true;
                }
                catch
                {
                    // Failed to import this file, continue
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}