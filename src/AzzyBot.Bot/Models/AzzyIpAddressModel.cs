namespace AzzyBot.Bot.Models;

/// <summary>
/// Represents an IP address record.
/// </summary>
public sealed record class AzzyIpAddressModel
{
    /// <summary>
    /// The IPv4 address.
    /// </summary>
    public string Ipv4 { get; init; }

    /// <summary>
    /// The IPv6 address.
    /// </summary>
    public string Ipv6 { get; init; }

    public AzzyIpAddressModel(string ipv4, string ipv6)
    {
        Ipv4 = ipv4;
        Ipv6 = ipv6;
    }
}
