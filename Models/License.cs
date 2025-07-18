using System;

namespace RvtToNavisConverter.Models
{
    public class License
    {
        public string HardwareId { get; set; }
        public DateTime TrialStartDate { get; set; }
        public int TrialDays { get; set; }
        public string LicenseType { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string Signature { get; set; }
        
        // Legacy field for backward compatibility
        public string Checksum { get; set; }
        
        public License()
        {
            TrialDays = 30; // Default 30-day trial
            LicenseType = "Trial";
            CustomerName = string.Empty;
            CustomerEmail = string.Empty;
            Signature = string.Empty;
            Checksum = string.Empty;
        }
    }

    public enum LicenseStatus
    {
        Valid,
        Expired,
        Invalid,
        Tampered,
        NotFound
    }

    public class LicenseValidationResult
    {
        public LicenseStatus Status { get; set; }
        public int DaysRemaining { get; set; }
        public string Message { get; set; }
        public string HardwareId { get; set; }
    }
}