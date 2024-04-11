using System;
using System.Collections.Generic;
using System.Reflection;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.ClubManagement;
using AzzyBot.Modules.Core.Models;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.MusicStreaming;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.Core;

/// <summary>
/// Contains methods for generating help information for commands.
/// </summary>
internal static class CoreAzzyHelp
{
    /// <summary>
    /// Gets a list of formatted commands for a given type.
    /// </summary>
    /// <param name="type">The type to get the commands from.</param>
    /// <param name="parentCommandGroup">The parent command group.</param>
    /// <returns>A list of formatted commands.</returns>
    private static List<AzzyHelpModel> GetFormattedCommands(Type type, string parentCommandGroup = "")
    {
        List<AzzyHelpModel> formattedCommands = [];

        // Check if the type is a top-level command group by checking if the parent is ApplicationCommandModule.
        bool isTopLevelGroup = type.IsSubclassOf(typeof(ApplicationCommandModule)) && !type.IsNested;

        // If it's a top-level group, process the command group attribute.
        if (isTopLevelGroup)
        {
            SlashCommandGroupAttribute? commandGroupAttribute = type.GetCustomAttribute<SlashCommandGroupAttribute>();
            if (commandGroupAttribute is not null)
                parentCommandGroup = commandGroupAttribute.Name; // Set the base for nested commands
        }

        // Process nested command groups if any
        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            SlashCommandGroupAttribute? nestedCommandGroupAttribute = nestedType.GetCustomAttribute<SlashCommandGroupAttribute>();
            if (nestedCommandGroupAttribute is not null)
            {
                string fullCommandGroupName = $"{parentCommandGroup} {nestedCommandGroupAttribute.Name}".Trim();
                formattedCommands.AddRange(GetFormattedCommands(nestedType, fullCommandGroupName)); // Recursive call
            }
        }

        // Process commands only if it's not a top-level group.
        if (!isTopLevelGroup)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                SlashCommandAttribute? commandAttribute = method.GetCustomAttribute<SlashCommandAttribute>();
                if (commandAttribute is not null)
                {
                    AzzyHelpModel commandInfo = new()
                    {
                        Name = $"{parentCommandGroup} {commandAttribute.Name}".Trim(),
                        Description = commandAttribute.Description
                    };

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        if (parameter.ParameterType != typeof(InteractionContext))
                        {
                            OptionAttribute? optionAttribute = parameter.GetCustomAttribute<OptionAttribute>();

                            if (optionAttribute is not null)
                            {
                                string parameterName = optionAttribute.Name;
                                if (parameter.HasDefaultValue)
                                {
                                    parameterName += " (optional)";
                                }
                                else
                                {
                                    parameterName += " (required)";
                                }

                                commandInfo.Parameters.Add((parameterName, optionAttribute.Description));
                            }
                        }
                    }

                    formattedCommands.Add(commandInfo);
                }
            }
        }

        return formattedCommands;
    }

    /// <summary>
    /// Gets a list of allowed command types based on the activated modules.
    /// </summary>
    /// <returns>A list of allowed command types.</returns>
    private static List<Type> AllowedCommandTypes()
    {
        List<Type> allowedCommands = [typeof(CoreCommands)];

        if (ModuleStates.AzuraCast)
            allowedCommands.Add(typeof(AzuraCastCommands));

        if (ModuleStates.AzuraCast && ModuleStates.ClubManagement)
            allowedCommands.Add(typeof(ClubManagementCommands));

        if (ModuleStates.AzuraCast && ModuleStates.MusicStreaming)
            allowedCommands.Add(typeof(MsCommands));

        return allowedCommands;
    }

    /// <summary>
    /// Checks if a user has the permission to see the command.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="member">The user to check.</param>
    /// <returns>true if the user should see the command; otherwise, false.</returns>
    private static bool CheckIfUserShouldSeeTheCommand(string commandName, DiscordMember member)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        ArgumentNullException.ThrowIfNull(member);

        return commandName switch
        {
            "azuracast ping" or "config bot-restart" or "core info azzy" or "core ping azzy" => CoreDiscordCommands.CheckIfUserHasRole(member, CoreSettings.AdminRoleId),
            "azuracast switch-playlists" or "staff close-club" or "staff open-club" => CoreDiscordCommands.CheckIfUserHasRole(member, CoreSettings.AdminRoleId) || CoreModule.CheckIfUserHasStaffRole(member),
            _ => true,
        };
    }

    /// <summary>
    /// Gets a list of commands and their descriptions.
    /// </summary>
    /// <param name="caller">The user calling the command.</param>
    /// <returns>A list of commands and their descriptions.</returns>
    internal static List<AzzyHelpModel> GetCommandsAndDescriptions(DiscordMember caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        List<AzzyHelpModel> allCommands = [];

        foreach (Type type in AllowedCommandTypes())
        {
            if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(ApplicationCommandModule)) && !type.IsNested)
            {
                foreach (AzzyHelpModel model in GetFormattedCommands(type))
                {
                    if (!CheckIfUserShouldSeeTheCommand(model.Name, caller))
                        continue;

                    allCommands.Add(model);
                }
            }
        }

        return allCommands;
    }

    /// <summary>
    /// Gets the details of a single command.
    /// </summary>
    /// <param name="caller">The user calling the command.</param>
    /// <param name="commandName">The name of the command.</param>
    /// <returns>The details of the command.</returns>
    /// <exception cref="InvalidOperationException">Throws when the command could not be found.</exception>
    internal static AzzyHelpModel GetSingleCommandDetails(DiscordMember caller, string commandName)
    {
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        foreach (AzzyHelpModel model in GetCommandsAndDescriptions(caller))
        {
            if (model.Name == commandName)
                return model;
        }

        throw new InvalidOperationException("The requested command was not found!");
    }
}
