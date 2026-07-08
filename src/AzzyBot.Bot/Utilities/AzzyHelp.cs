using System;
using System.Collections.Generic;
using System.Linq;

using AzzyBot.Bot.Models;

using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Utilities;

public static class AzzyHelp
{
    private static bool CheckIfMemberHasPermission(bool adminServer, bool approvedDebug, DiscordMember member, string command)
    {
        DiscordPermissions permissions = member.Permissions;

        return command switch
        {
            "admin" => adminServer,
            "azuracast" => permissions.HasPermission(DiscordPermission.Administrator),
            "config" => permissions.HasPermission(DiscordPermission.Administrator),
            "core" => true,
            "debug" => approvedDebug && permissions.HasPermission(DiscordPermission.Administrator),
            "dj" => true,
            "music" => true,
            "player" => true,
            _ => false
        };
    }

    public static IReadOnlyDictionary<string, List<AzzyHelpModel>> GetAllCommands(IReadOnlyDictionary<string, Command> commands, bool adminServer, bool approvedDebug, DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentOutOfRangeException.ThrowIfZero(commands.Count);

        return GetCommandGroups(commands, adminServer, approvedDebug, member);
    }

    public static AzzyHelpModel GetSingleCommand(IReadOnlyDictionary<string, Command> commands, string commandName, bool adminServer, bool approvedDebug, DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentOutOfRangeException.ThrowIfZero(commands.Count);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        string[] parts = commandName.Split(' ');

        return GetCommandGroups(commands, adminServer, approvedDebug, member, singleCommand: true).Where(c => c.Key == parts[0]).SelectMany(static r => r.Value).FirstOrDefault(c => c.Name == commandName) ?? throw new InvalidOperationException($"Command not found: {commandName}");
    }

    private static Dictionary<string, List<AzzyHelpModel>> GetCommandGroups(IReadOnlyDictionary<string, Command> commands, bool adminServer, bool approvedDebug, DiscordMember member, bool singleCommand = false)
    {
        List<string> commandGroups = [.. commands.Where(c => c.Value.Subcommands.Count > 0 && CheckIfMemberHasPermission(adminServer, approvedDebug, member, c.Value.Name)).Select(static c => c.Value.Name)];
        Dictionary<string, List<AzzyHelpModel>> groupedCommands = new(commands.Count);
        foreach (string group in commandGroups)
        {
            Command command = commands[group];
            List<AzzyHelpModel> subCommands = GetCommands([.. command.Subcommands.Where(c => c.Description is not "No description provided.")], command.Name, singleCommand);
            foreach (Command subCommand in command.Subcommands.Where(static c => c.Subcommands.Count > 0))
            {
                subCommands.AddRange(GetCommands(subCommand.Subcommands, command.Name, singleCommand));
            }

            groupedCommands.Add(group, subCommands);
        }

        return groupedCommands;
    }

    private static List<AzzyHelpModel> GetCommands(IReadOnlyList<Command> commands, string subCommand = "", bool singleCommand = false)
    {
        List<AzzyHelpModel> models = new(commands.Count);
        foreach (Command command in commands)
        {
            string description = command.Description ?? "No description provided.";
            Dictionary<string, string> parameters = [];
            if (singleCommand)
                parameters = GetCommandParameters(command);

            models.Add(new AzzyHelpModel(subCommand, command.FullName, description, parameters));
        }

        return models;
    }

    private static Dictionary<string, string> GetCommandParameters(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        Dictionary<string, string> parameters = new(command.Parameters.Count);
        foreach (CommandParameter parameter in command.Parameters)
        {
            string paramName = parameter.Name ?? "No name provided";
            string paramDescription = parameter.Description ?? "No description provided.";
            if (parameter.DefaultValue.HasValue)
            {
                paramName += " (optional)";
            }
            else
            {
                paramName += " (required)";
            }

            parameters.Add(paramName, paramDescription);
        }

        return parameters;
    }
}
