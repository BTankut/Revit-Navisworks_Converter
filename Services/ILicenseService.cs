using RvtToNavisConverter.Models;

namespace RvtToNavisConverter.Services
{
    public interface ILicenseService
    {
        LicenseValidationResult ValidateLicense();
        LicenseValidationResult CreateTrialLicense();
        void SaveLicense(License license);
        License LoadLicense();
        void ClearLicense();
        bool CheckAndImportLicenseFile();
    }
}