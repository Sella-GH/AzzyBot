using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;

using TagLibSharp2.Core;

namespace AzzyBot.Bot.Utilities;

public static class AzuraFileChecker
{
    public static async Task<AzuraFileComplianceModel> FileIsCompliantAsync(string path)
    {
        MediaFileResult media = await MediaFile.ReadAsync(path);

        bool titleCompliant = !string.IsNullOrWhiteSpace(media.Tag?.Title);
        bool artistCompliant = !string.IsNullOrWhiteSpace(media.Tag?.Artist);
        bool isCompliant = titleCompliant && artistCompliant;

        return new AzuraFileComplianceModel(isCompliant, titleCompliant, artistCompliant);
    }
}
