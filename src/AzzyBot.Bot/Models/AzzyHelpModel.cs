using System.Collections.Generic;

namespace AzzyBot.Bot.Models;

/// <summary>
/// Represents a help record for a command.
/// </summary>
public sealed record class AzzyHelpModel
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
    public IReadOnlyDictionary<string, string> Parameters { get; init; }

    public AzzyHelpModel(string subCommand, string name, string description, IReadOnlyDictionary<string, string> parameters)
    {
        SubCommand = subCommand;
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}
