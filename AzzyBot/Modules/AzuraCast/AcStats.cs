using System;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.Core;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast;

internal static class AcStats
{
    internal static async Task<DiscordEmbed> GetServerStatsAsync(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.admin, AcApiEnum.server, AcApiEnum.stats);

        string ping = await CoreWebRequests.GetPingTimeAsync(AcSettings.AzuraApiUrl);
        if (string.IsNullOrWhiteSpace(ping))
            return AcEmbedBuilder.BuildServerIsOfflineEmbed(userName, userAvatarUrl, false);

        string body = await CoreWebRequests.GetWebAsync(url, AcServer.Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty!");

        SystemData? data = JsonConvert.DeserializeObject<SystemData>(body);

        if (data is null)
            throw new InvalidOperationException($"{nameof(data)} is null!");

        string cpuUsageTotal = data.Cpu.Total.Usage;
        string[] cpuUsageCores;
        double[] cpuUsageTimes;
        double memoryTotal = Math.Round(double.Parse(data.Memory.Bytes.Total, CultureInfo.InvariantCulture) / (1024.0 * 1024.0 * 1024.0), 2);
        double memoryUsed = Math.Round(double.Parse(data.Memory.Bytes.Used, CultureInfo.InvariantCulture) / (1024.0 * 1024.0 * 1024.0), 2);
        double memoryCached = Math.Round(double.Parse(data.Memory.Bytes.Cached, CultureInfo.InvariantCulture) / (1024.0 * 1024.0 * 1024.0), 2);
        double memoryUsedTotal = Math.Round(memoryUsed + memoryCached, 2);
        double diskTotal = Math.Round(double.Parse(data.Disk.Bytes.Total, CultureInfo.InvariantCulture) / (1024.0 * 1024.0 * 1024.0), 2);
        double diskUsed = Math.Round(double.Parse(data.Disk.Bytes.Used, CultureInfo.InvariantCulture) / (1024.0 * 1024.0 * 1024.0), 2);
        string[] networks;
        double[] networkRXspeed;
        double[] networkTXspeed;

        if (data.Cpu.Cores.Count > 0)
        {
            cpuUsageCores = new string[data.Cpu.Cores.Count];

            for (int i = 0; i < data.Cpu.Cores.Count; i++)
            {
                //
                // CPU Core positions
                // Pos 0 = CPU 0
                // Pos 1 = CPU 1
                //

                cpuUsageCores[i] = Math.Round(decimal.Parse(data.Cpu.Cores[i].Usage, CultureInfo.InvariantCulture), 2).ToString(CultureInfo.InvariantCulture);
            }
        }
        else
        {
            throw new InvalidOperationException("No cpu cores found!");
        }

        if (data.Cpu.Load.Count > 0)
        {
            cpuUsageTimes = new double[data.Cpu.Load.Count];

            for (int i = 0; i < data.Cpu.Load.Count; i++)
            {
                //
                // Average Loading positions
                // Pos 0 = 1-Min-Load
                // Pos 1 = 5-Min-Load
                // Pos 2 = 10-Min-Load
                //

                cpuUsageTimes[i] = Math.Round(data.Cpu.Load[i], 2);
            }
        }
        else
        {
            throw new InvalidOperationException("No average cpu load found!");
        }

        if (data.Network.Count > 0)
        {
            networks = new string[data.Network.Count];
            networkRXspeed = new double[data.Network.Count];
            networkTXspeed = new double[data.Network.Count];

            for (int i = 0; i < data.Network.Count; i++)
            {
                //
                // Network positions
                // Pos 0 = Network "internal" (docker)
                // Pos 1 = Network "external" (server)
                //

                networks[i] = data.Network[i].InterfaceName;
                networkRXspeed[i] = Math.Round(double.Parse(data.Network[i].Received.Speed.Bytes, CultureInfo.InvariantCulture) / 1024.0, 2);
                networkTXspeed[i] = Math.Round(double.Parse(data.Network[i].Transmitted.Speed.Bytes, CultureInfo.InvariantCulture) / 1024.0, 2);
            }
        }
        else
        {
            throw new InvalidOperationException("No networks found!");
        }

        return AcEmbedBuilder.BuildMusicServerStatsEmbed(userName, userAvatarUrl, ping, cpuUsageTotal, cpuUsageCores, cpuUsageTimes, memoryTotal, memoryUsed, memoryCached, memoryUsedTotal, diskTotal, diskUsed, networks, networkRXspeed, networkTXspeed);
    }
}
