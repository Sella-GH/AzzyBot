using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace AzzyBot.Web.Components.Pages;

public partial class Error : ComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId
        => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
        => RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
