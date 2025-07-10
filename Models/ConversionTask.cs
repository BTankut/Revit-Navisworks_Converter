using System.Collections.Generic;

namespace RvtToNavisConverter.Models
{
    public class ConversionTask
    {
        public List<FileItem> FilesToProcess { get; set; }
        public string OutputNwdFile { get; set; } = string.Empty;
        public bool OverwriteExisting { get; set; }
        public string LogFilePath { get; set; } = string.Empty;
        // Add other Navisworks options as properties here
        public string OutputVersion { get; set; } = string.Empty;
        public bool OpenAfterConversion { get; set; }

        public ConversionTask()
        {
            FilesToProcess = new List<FileItem>();
        }
    }
}
