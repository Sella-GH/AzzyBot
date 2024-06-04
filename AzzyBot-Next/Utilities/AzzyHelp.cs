using System;
using System.Collections.Generic;
using AzzyBot.Utilities.Records;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Utilities;

public static class AzzyHelp
{
    private static bool CheckIfMemberHasPermission(bool adminServer, bool approvedDebug, DiscordMember member, string command)
    {
        DiscordPermissions permissions = member.Permissions;

        return command switch
        {
            "admin" => adminServer,
            "config" => permissions.HasPermission(DiscordPermissions.Administrator),
            "core" => true,
            "debug" => approvedDebug && permissions.HasPermission(DiscordPermissions.Administrator),
            _ => false,
        };
    }

    public static IReadOnlyList<AzzyHelpRecord> GetAllCommands(IReadOnlyDictionary<string, Command> commands, bool adminServer, bool approvedDebug, DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(commands, nameof(commands));
        ArgumentOutOfRangeException.ThrowIfZero(commands.Count, nameof(commands));

        return GetCommandGroups(commands, adminServer, approvedDebug, member);
    }

    public static AzzyHelpRecord GetSingleCommand(IReadOnlyDictionary<string, Command> commands, string commandName, bool adminServer, bool approvedDebug, DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(commands, nameof(commands));
        ArgumentOutOfRangeException.ThrowIfZero(commands.Count, nameof(commands));
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName, nameof(commandName));

        return GetCommandGroups(commands, adminServer, approvedDebug, member).Find(record => record.Name == commandName) ?? throw new InvalidOperationException("No command found!");
    }

    private static List<AzzyHelpRecord> GetCommandGroups(IReadOnlyDictionary<string, Command> commands, bool adminServer, bool approvedDebug, DiscordMember member)
    {
        List<AzzyHelpRecord> records = [];
        foreach (KeyValuePair<string, Command> kvp in commands)
        {
            Command command = kvp.Value;
            string subCommand = command.Name;
            if (command.Subcommands.Count > 0)
            {
                if (!CheckIfMemberHasPermission(adminServer, approvedDebug, member, command.Name))
                    continue;

                records.AddRange(GetCommands(command.Subcommands, subCommand));
            }
            else
            {
                records.AddRange(GetCommands([command]));
            }
        }

        return records;
    }

    private static List<AzzyHelpRecord> GetCommands(IReadOnlyList<Command> commands, string subCommand = "")
    {
        List<AzzyHelpRecord> records = [];
        foreach (Command command in commands)
        {
            string description = command.Description ?? "No description provided";
            Dictionary<string, string> parameters = [];
            foreach (CommandParameter parameter in command.Parameters)
            {
                string paramName = parameter.Name ?? "No name provided";
                string paramDescription = parameter.Description ?? "No description provided";
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

            records.Add(new AzzyHelpRecord(subCommand, command.Name, description, parameters));
        }

        return records;
    }
}
