using System.Net;

namespace AzzyBot.Bot.Utilities.Helpers;

/// <summary>
/// Provides utility helpers to classify HTTP status codes for network error handling paths.
/// </summary>
public static class HttpStatusCodeHelpers
{
    /// <summary>
    /// Determines whether the status code indicates an upstream or backend availability failure.
    /// </summary>
    /// <param name="status">The HTTP status code to classify.</param>
    /// <returns><see langword="true"/> when the status should be treated as a server-down condition; otherwise <see langword="false"/>.</returns>
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
