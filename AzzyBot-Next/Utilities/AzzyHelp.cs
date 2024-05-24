using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AzzyBot.Commands;
using AzzyBot.Utilities.Records;
using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace AzzyBot.Utilities;

public static class AzzyHelp
{
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "It's just to get the name fully normalized")]
    private static List<AzzyHelpRecord> GetAllCommandsOfType(Type type)
    {
        List<AzzyHelpRecord> commands = [];

        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Instance))
        {
            string parentCommand = nestedType.Name;

            foreach (MethodInfo method in nestedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                CommandAttribute? command = method.GetCustomAttribute<CommandAttribute>();

                if (command is null)
                    continue;

                string commandName = $"/{parentCommand.ToLowerInvariant()} {command.Name}";
                string description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description provided";

                Dictionary<string, string> parameters = [];
                foreach (ParameterInfo parameter in method.GetParameters().Where(p => p.ParameterType != typeof(CommandContext)))
                {
                    string paramName = parameter.Name ?? "No name provided";
                    string paramDescription = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description provided";
                    if (parameter.HasDefaultValue)
                    {
                        paramName += " (optional)";
                    }
                    else
                    {
                        paramName += " (required)";
                    }

                    parameters.Add(paramName, paramDescription);
                }

                AzzyHelpRecord commandInfo = new(parentCommand, commandName, description, parameters);
                commands.Add(commandInfo);
            }
        }

        return commands;
    }

    private static bool CheckIfMemberHasPermission(bool adminServer, bool approvedDebug, DiscordMember member, Type type)
    {
        DiscordPermissions permissions = member.Permissions;

        return type.Name switch
        {
            nameof(AdminCommands) => adminServer,
            nameof(ConfigCommands) => permissions.HasPermission(DiscordPermissions.Administrator),
            nameof(CoreCommands) => true,
            nameof(DebugCommands) => approvedDebug && permissions.HasPermission(DiscordPermissions.Administrator),
            _ => false,
        };
    }

    public static Dictionary<int, List<AzzyHelpRecord>> GetCommands(bool adminServer, bool approvedDebug, DiscordMember member)
    {
        Dictionary<int, List<AzzyHelpRecord>> records = [];
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "AzzyBot.Commands" && CheckIfMemberHasPermission(adminServer, approvedDebug, member, t)))
        {
            records.Add(records.Count, GetAllCommandsOfType(type));
        }

        return records;
    }

    public static AzzyHelpRecord GetSingleCommand(bool adminServer, bool approvedDebug, DiscordMember member, string commandName)
    {
        foreach (KeyValuePair<int, List<AzzyHelpRecord>> kvp in GetCommands(adminServer, approvedDebug, member))
        {
            return kvp.Value.First(c => c.Name == commandName);
        }

        throw new InvalidOperationException("No command found!");
    }
}
