using System.Collections.Generic;

namespace RvtToNavisConverter.Services
{
    public class FileStatusService : IFileStatusService
    {
        private readonly Dictionary<string, string> _statuses = new Dictionary<string, string>();

        public string GetStatus(string filePath)
        {
            return _statuses.TryGetValue(filePath, out var status) ? status : "Pending";
        }

        public void SetStatus(string filePath, string status)
        {
            _statuses[filePath] = status;
        }

        public void Reset()
        {
            _statuses.Clear();
        }
    }
}
