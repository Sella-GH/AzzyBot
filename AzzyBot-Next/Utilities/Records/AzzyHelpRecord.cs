using System.Collections.Generic;

namespace AzzyBot.Utilities.Records;

public sealed record AzzyHelpRecord
{
    public string SubCommand { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public Dictionary<string, string> Parameters { get; init; }

    public AzzyHelpRecord(string subCommand, string name, string description, Dictionary<string, string> parameters)
    {
        SubCommand = subCommand;
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}
