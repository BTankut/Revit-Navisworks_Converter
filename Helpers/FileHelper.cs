using System.IO;

namespace RvtToNavisConverter.Helpers
{
    public class FileHelper
    {
        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
