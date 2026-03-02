using System.Threading.Tasks;

using AzzyBot.Bot.Utilities.Records.AzuraCast;

using TagLibSharp2.Core;

namespace AzzyBot.Bot.Utilities;

public static class AzuraFileChecker
{
    public static async Task<AzuraFileComplianceRecord> FileIsCompliantAsync(string path)
    {
        MediaFileResult media = await MediaFile.ReadAsync(path);

        bool titleCompliant = !string.IsNullOrWhiteSpace(media.Tag?.Title);
        bool artistCompliant = !string.IsNullOrWhiteSpace(media.Tag?.Artist);
        bool isCompliant = titleCompliant && artistCompliant;

        return new AzuraFileComplianceRecord(isCompliant, titleCompliant, artistCompliant);
    }
}
