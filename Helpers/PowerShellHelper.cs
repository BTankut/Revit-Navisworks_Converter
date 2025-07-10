using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Helpers
{
    public class PowerShellHelper
    {
        public event Action<string>? CommandLog;

        public Task<string> RunScriptAsync(string scriptPath, string arguments)
        {
            var script = $"& '{scriptPath}' {arguments}";
            return RunScriptInternal(script);
        }

        public Task<string> RunScriptStringAsync(string scriptString)
        {
            return RunScriptInternal(scriptString);
        }

        private Task<string> RunScriptInternal(string script)
        {
            return Task.Run(() =>
            {
                // Log the command being executed
                CommandLog?.Invoke($"Executing PowerShell command:");
                CommandLog?.Invoke(script);
                CommandLog?.Invoke("----------------------------------------------");

                var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var process = new Process { StartInfo = processStartInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                        CommandLog?.Invoke(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                        CommandLog?.Invoke($"ERROR: {args.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                CommandLog?.Invoke("----------------------------------------------");
                
                if (process.ExitCode != 0)
                {
                    var errorMessage = $"PowerShell script failed with exit code {process.ExitCode}.\nError: {error}";
                    CommandLog?.Invoke(errorMessage);
                    // We return the error output as the result in case of failure
                    return error.ToString();
                }
                else
                {
                    CommandLog?.Invoke($"PowerShell script completed successfully.");
                }

                return output.ToString();
            });
        }
    }
}
