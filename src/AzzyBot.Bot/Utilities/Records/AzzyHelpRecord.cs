using System.Collections.Generic;

namespace AzzyBot.Bot.Utilities.Records;

/// <summary>
/// Represents a help record for a command.
/// </summary>
public sealed record AzzyHelpRecord
{
    /// <summary>
    /// The subcommand for the help record.
    /// </summary>
    public string SubCommand { get; init; }

    /// <summary>
    /// The name of the command.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The description of the command.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// The parameters for the command.
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; }

    public AzzyHelpRecord(string subCommand, string name, string description, Dictionary<string, string> parameters)
    {
        SubCommand = subCommand;
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}
