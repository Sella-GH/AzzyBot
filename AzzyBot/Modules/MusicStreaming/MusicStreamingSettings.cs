using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MusicStreamingSettings : BaseSettings
{
    internal static bool MusicStreamingSettingsLoaded { get; private set; }
    internal static bool ActivateLyrics { get; private set; }
    internal static string GeniusApiKey { get; private set; } = string.Empty;
    internal static bool AutoDisconnect { get; private set; }
    internal static int AutoDisconnectTime { get; private set; }
    internal static string MountPointStub { get; private set; } = string.Empty;
    internal static bool DeleteLavalinkLogs { get; private set; }

    internal static async Task<bool> LoadMusicStreamingAsync()
    {
        ArgumentNullException.ThrowIfNull(Config);

        ActivateLyrics = Convert.ToBoolean(Config["MusicStreaming:ActivateLyrics"], CultureInfo.InvariantCulture);
        GeniusApiKey = await GetGeniusApiKeyAsync();
        AutoDisconnect = Convert.ToBoolean(Config["MusicStreaming:AutoDisconnect"], CultureInfo.InvariantCulture);
        AutoDisconnectTime = Convert.ToInt32(Config["MusicStreaming:AutoDisconnectTime"], CultureInfo.InvariantCulture);
        MountPointStub = Config["MusicStreaming:MountPointStub"] ?? string.Empty;
        DeleteLavalinkLogs = Convert.ToBoolean(Config["MusicStreaming:DeleteLavalinkLogs"], CultureInfo.InvariantCulture);

        return MusicStreamingSettingsLoaded = CheckSettings(typeof(MusicStreamingSettings));
    }

    private static async Task<string> GetGeniusApiKeyAsync()
    {
        await Console.Out.WriteLineAsync("Getting Genius API key");
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files", "application.yml");

        if (!File.Exists(path))
            return string.Empty;

        try
        {
            string[] lines = await File.ReadAllLinesAsync(path);
            string lineBefore = string.Empty;
            string lineAfter = string.Empty;
            string password = string.Empty;

            for (int i = 0; lines.Length > i; i++)
            {
                string line = lines[i].Trim();

                if (i == lines.Length - 1)
                    break;

                if (i > 1)
                    lineBefore = lines[i - 1].Trim();

                lineAfter = lines[i + 1].Trim();

                if (line.StartsWith("geniusApiKey:", StringComparison.OrdinalIgnoreCase) && lineBefore.StartsWith("countryCode:", StringComparison.OrdinalIgnoreCase) && lineAfter.StartsWith("lavalink:", StringComparison.OrdinalIgnoreCase))
                {
                    password = line.Split(':')[1].Trim().Trim('\"');
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("The Genius api key couldn't be found");

            return password;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
