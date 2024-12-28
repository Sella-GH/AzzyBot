namespace AzzyBot.Core.Utilities.Helpers;

public static class FileSizes
{
    /// <summary>
    /// The maximum file size for the file which is supposed to be uploaded to AzuraCast.
    /// </summary>
    /// <remarks>
    /// 52323942 bytes (~49.9 MB)
    /// </remarks>
    public const int AzuraFileSize = 52323942;

    /// <summary>
    /// The maximum file size for the file which is supposed to be uploaded to Discord.
    /// </summary>
    /// <remarks>
    /// 10380902 bytes (~9.9 MB)
    /// </remarks>
    public const int DiscordFileSize = 10380902;
}
