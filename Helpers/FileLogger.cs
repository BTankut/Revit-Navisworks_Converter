using System;
using System.IO;

namespace RvtToNavisConverter.Helpers
{
    public static class FileLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "app_log.txt");

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public static void Log(Exception ex)
        {
            try
            {
                var errorDetails = $@"
===========================================================
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Error: {ex.Message}
Stack Trace:
{ex.StackTrace}
Inner Exception:
{ex.InnerException?.ToString() ?? "None"}
===========================================================
";
                File.AppendAllText(LogFilePath, errorDetails);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public static void LogError(string message)
        {
            Log($"ERROR: {message}");
        }

        public static void LogWarning(string message)
        {
            Log($"WARNING: {message}");
        }
    }
}
