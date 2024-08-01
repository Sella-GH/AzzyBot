using AzzyBot.Bot.Utilities.Records.AzuraCast;
using TagLib;

namespace AzzyBot.Bot.Utilities;

public static class AzuraFileChecker
{
    public static AzuraFileComplianceRecord FileIsCompliant(string path)
    {
        using File tFile = File.Create(path);

        bool titleCompliant = !string.IsNullOrWhiteSpace(tFile.Tag.Title);
        bool artistCompliant = !string.IsNullOrWhiteSpace(tFile.Tag.FirstPerformer);
        bool isCompliant = titleCompliant && artistCompliant;

        return new AzuraFileComplianceRecord(isCompliant, titleCompliant, artistCompliant);
    }
}
