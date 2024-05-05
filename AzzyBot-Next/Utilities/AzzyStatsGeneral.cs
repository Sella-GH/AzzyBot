using System.Diagnostics;
using System.IO;
using AzzyBot.Utilities.Records;

namespace AzzyBot.Utilities;

internal sealed class AzzyStatsGeneral
{
    #region EmbedAzzyStats

    internal static double GetBotMemoryUsage()
    {
        using Process? process = Process.GetCurrentProcess();
        return process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0);
    }

    internal static DiskUsageRecord GetDiskUsage()
    {
        try
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == "/")
                {
                    double totalSize = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double totalFreeSpace = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double totalUsedSpace = totalSize - totalFreeSpace;

                    return new(totalSize, totalFreeSpace, totalUsedSpace);
                }
            }

            return new(0, 0, 0);
        }
        catch (DriveNotFoundException)
        {
            throw;
        }
    }

    #endregion EmbedAzzyStats
}
