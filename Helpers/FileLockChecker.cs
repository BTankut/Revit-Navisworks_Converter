using System;
using System.IO;
using System.Threading;

namespace RvtToNavisConverter.Helpers
{
    public static class FileLockChecker
    {
        /// <summary>
        /// Checks if a file is locked by another process
        /// </summary>
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // The file is unavailable because it is:
                // - Still being written to
                // - Being processed by another thread
                // - Does not exist (has already been processed)
                return true;
            }

            return false;
        }

        /// <summary>
        /// Waits for a file to be available, with timeout
        /// </summary>
        public static bool WaitForFile(string filePath, int timeoutSeconds = 30)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            while (IsFileLocked(filePath))
            {
                if (DateTime.Now - startTime > timeout)
                {
                    return false; // Timeout reached
                }
                Thread.Sleep(500); // Wait 500ms before checking again
            }

            return true; // File is now available
        }
    }
}