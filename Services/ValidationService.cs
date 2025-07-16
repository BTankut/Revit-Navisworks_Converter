using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public enum ValidationStatus
    {
        None,
        Valid,
        Invalid,
        Warning
    }

    public interface IValidationService
    {
        Task<ValidationStatus> ValidateIpAddressAsync(string ipAddress);
        ValidationStatus ValidatePath(string path, bool isFile);
    }

    public class ValidationService : IValidationService
    {
        public async Task<ValidationStatus> ValidateIpAddressAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return ValidationStatus.None;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 1000); // 1 second timeout
                return reply.Status == IPStatus.Success ? ValidationStatus.Valid : ValidationStatus.Invalid;
            }
            catch (PingException)
            {
                return ValidationStatus.Invalid;
            }
        }

        public ValidationStatus ValidatePath(string path, bool isFile)
        {
            if (string.IsNullOrWhiteSpace(path))
                return ValidationStatus.None;

            if (isFile)
            {
                return File.Exists(path) ? ValidationStatus.Valid : ValidationStatus.Invalid;
            }
            else
            {
                if (Directory.Exists(path))
                {
                    return ValidationStatus.Valid;
                }
                
                // Check if the parent directory exists to determine if we can create it
                var parent = Directory.GetParent(path);
                return parent != null && parent.Exists ? ValidationStatus.Warning : ValidationStatus.Invalid;
            }
        }
    }
}
