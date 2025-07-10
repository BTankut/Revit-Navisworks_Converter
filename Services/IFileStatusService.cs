using System.Collections.Generic;

namespace RvtToNavisConverter.Services
{
    public interface IFileStatusService
    {
        string GetStatus(string filePath);
        void SetStatus(string filePath, string status);
        void Reset();
    }
}
