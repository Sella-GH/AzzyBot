using System.Collections.Generic;

namespace AzzyBot.Modules.Core.Models;

internal sealed class AzzyHelpModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<(string ParameterName, string Description)> Parameters { get; set; } = [];
}
