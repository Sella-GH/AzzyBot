using System.Diagnostics;
using System.Threading.Tasks;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingLavalink
{
    internal static async Task<bool> CheckIfJavaIsInstalledAsync()
    {
        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = "java",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(processStartInfo);

            if (process is null)
                return false;

            string? output = await process.StandardOutput.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(output))
                return false;

            string[] javaString = output.Split(' ')[1].Split('.');

            return javaString[0] is "17" or "21";
        }
        catch
        {
            return false;
            throw;
        }
    }
}
