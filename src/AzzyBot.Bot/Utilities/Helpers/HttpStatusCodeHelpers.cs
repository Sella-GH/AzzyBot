using System.Net;

namespace AzzyBot.Bot.Utilities.Helpers;

public static class HttpStatusCodeHelpers
{
    public static bool IsServerDownStatus(HttpStatusCode status)
    {
        return (int)status switch
        {
            502 or
            503 or
            504 or
            520 or
            521 or
            522 or
            523 or
            524 or
            525 or
            526 or
            530 => true,
            _ => false
        };
    }
}
