using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.Core;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Commands.Attributes;

/// <summary>
/// Checks if the AzuraCast station is up or not.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class RequireMusicStationUp : SlashCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        AcStationModel station = await AcServer.GetStationDataAsync();

        HttpClient client = (AcSettings.Ipv6Available) ? CoreWebRequests.GetHttpClient(AcServer.Headers) : CoreWebRequests.GetHttpClientV4(AcServer.Headers);
        using HttpResponseMessage response = await client.GetAsync(new Uri(station.ListenUrl));
        if (response.StatusCode != HttpStatusCode.OK)
            return false;

        NowPlayingData nowPlaying = await AcServer.GetNowPlayingAsync();

        return nowPlaying.IsOnline;
    }
}
