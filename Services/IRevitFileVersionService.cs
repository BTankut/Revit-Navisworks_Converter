using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public interface IRevitFileVersionService
    {
        Task<string> GetRevitFileVersionAsync(string filePath);
        string GetRevitVersionYear(string version);
        bool IsVersionCompatible(string fileVersion, string toolVersion);
        string GetRevitVersionFromServerPath(string serverPath);
    }
}