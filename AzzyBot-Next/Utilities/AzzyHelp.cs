using System;
using System.Collections.Generic;
using System.Linq;
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

    public static IReadOnlyDictionary<string, List<AzzyHelpRecord>> GetAllCommands(IReadOnlyDictionary<string, Command> commands, bool adminServer, bool approvedDebug, DiscordMember member)
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

        foreach (KeyValuePair<string, List<AzzyHelpRecord>> record in GetCommandGroups(commands, adminServer, approvedDebug, member))
        {
            foreach (AzzyHelpRecord subRecord in record.Value)
            {
                if (subRecord.Name == commandName)
                    return subRecord;
            }
        }

        throw new InvalidOperationException("Command not found");
    }

    private static Dictionary<string, List<AzzyHelpRecord>> GetCommandGroups(IReadOnlyDictionary<string, Command> commands, bool adminServer, bool approvedDebug, DiscordMember member)
    {
        List<string> commandGroups = [];
        foreach (Command command in commands.Where(c => c.Value.Subcommands.Count > 0).Select(c => c.Value))
        {
            if (!CheckIfMemberHasPermission(adminServer, approvedDebug, member, command.Name))
                continue;

            commandGroups.Add(command.Name);
        }

        Dictionary<string, List<AzzyHelpRecord>> records = [];
        foreach (string group in commandGroups)
        {
            records.Add(group, GetCommands(commands[group].Subcommands, group));
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

            records.Add(new AzzyHelpRecord(subCommand, command.FullName, description, parameters));
        }

        return records;
    }
}
