﻿using System;
using AzzyBot.Settings.MusicStreaming;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MusicStreamingModule : BaseModule
{
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<MusicStreamingCommands>(serverId);

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.LavalinkPassword:
                evt.ResultString = MusicStreamingSettings.LavalinkPassword;
                break;
        }
    }

    internal override async void StartProcesses()
    {
        if (!await MusicStreamingLavalink.CheckIfJavaIsInstalledAsync())
            throw new InvalidOperationException("You have to install Java/OpenJDK Runtime 17 or 21 first!");

        MusicStreamingLavalink.StartLavalink();
    }

    internal override async void StopProcesses() => await MusicStreamingLavalink.StopLavalinkAsync();
    internal override void Activate() => ModuleStates.ActivateMusicStreaming();
}
