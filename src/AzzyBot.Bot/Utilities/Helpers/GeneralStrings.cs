﻿namespace AzzyBot.Bot.Utilities.Helpers;

public static class GeneralStrings
{
    public const string AdminBotWideMessageEmpty = "You have to provide a message first!";
    public const string BotStatusChanged = "Bot status has been updated!";
    public const string BotStatusReset = "Bot status has been reset!";
    public const string CanNotLeaveServer = "You can't make me leave my own home server!";
    public const string CoreSettingsMissing = "You have to set the core settings first!";
    public const string CoreSettingsModified = "Core settings have been modified successfully.";
    public const string ConfigGet = "I sent you an overview with all the settings in private. Be aware of sensitive data.";
    public const string ConfigReset = "All settings have been reset successfully.\nRemember to set them up again!";
    public const string ConfigInstanceAdded = "Your AzuraCast installation was added successfully and private data has been encrypted.";
    public const string ConfigInstanceAdminMissing = "You have to select an instance admin group first!";
    public const string ConfigInstanceAlreadyExists = "AzuraCast is already set up for this server.";
    public const string ConfigInstanceDeleted = "Your AzuraCast installation was deleted successfully.";
    public const string ConfigInstanceModified = "Your AzuraCast installation was modified successfully and private data has been encrypted.";
    public const string ConfigInstanceModifiedChecks = "Your AzuraCast installation was modified successfully.";
    public const string ConfigInstanceNotificationChannelMissing = "You have to select a notification channel first!";
    public const string ConfigInstanceOutageChannelMissing = "You have to select an outage channel first!";
    public const string ConfigParameterMissing = "You have to provide at least one parameter first!";
    public const string ConfigStationAdded = "Your station was added successfully and private data has been encrypted.";
    public const string ConfigStationAdminMissing = "You have to select a station admin group first!";
    public const string ConfigStationDeleted = "Your station was deleted successfully.";
    public const string ConfigStationModified = "Your station was modified successfully and private data has been encrypted.";
    public const string ConfigStationModifiedChecks = "Your station was modified successfully.";
    public const string ConfigStationRequestChannelMissing = "You have to select a request channel first!";
    public const string DateFormatInvalid = "The date format is invalid. Please use the format: YYYY-MM-DD.";
    public const string FileTooBig = "The file is too big. Please upload a file smaller than 50MB.";
    public const string GuildIdInvalid = "This server ID is invalid.";
    public const string GuildNotFound = "This server does not exist in the database.";
    public const string HlsNotAvailable = "This station does not support HLS streaming.";
    public const string InstanceNotFound = "AzuraCast is not set up for this server.";
    public const string InstanceUpdateError = "An error occurred while checking for AzuraCast updates.";
    public const string InstanceUpToDate = "AzuraCast is already up to date.";
    public const string MessageSentToAll = "Your message has been sent to all servers.";
    public const string MountPointNotFound = "This mount point does not exist.";
    public const string NoGuildFound = "I'm not in any server.";
    public const string PlaylistEmpty = "This playlist is empty.";
    public const string PlaylistNotFound = "This playlist does not exist.";
    public const string PermissionIssue = "I don't have the required permissions to do this.";
    public const string SkipAlmostOver = "This song is almost over. Please wait!";
    public const string SkipToFast = "You can only skip a song every 30 seconds.";
    public const string SongRequestNotFound = "This song does not exist.";
    public const string SongRequestOffline = "Because local file caching is disabled, I can't request infos about songs while your instance is offline.";
    public const string SongRequestQueued = "Your song request has been queued.";
    public const string StationNotFound = "This station does not exist.";
    public const string StationOffline = "This station is currently offline.";
    public const string StationUsersDisconnected = "All users have been disconnected from the station.";
    public const string StreamProviderNotFound = "This stream provider does not exist.";
    public const string SystemLogEmpty = "This system log is empty and cannot be viewed.";
    public const string VoiceAlreadyIn = "I'm already in the voice channel.";
    public const string VoiceJoined = "I'm here now.";
    public const string VoiceLeft = "I'm gone now.";
    public const string VoiceNotConnected = "You are not connected to a voice channel.";
    public const string VoiceNoUser = "Discord didn't told me you're in a voice channel, please rejoin.";
    public const string VoiceNothingPlaying = "There is nothing playing right now.";
    public const string VoicePlayingAzuraCast = "The current played song is from an AzuraCast station. Please use `music now-playing` to get the information.";
    public const string VoicePlayMount = "I'm starting to play **%station%** now.";
    public const string VoicePlaySong = "I'm starting to play **%track%** by **%artist%** now.";
    public const string VoiceStationStopped = "The station has been stopped, I am stopping to play music.";
    public const string VoiceStop = "I'm stopping the music now.";
    public const string VoiceStopLeft = "I'm stopping the music and leaving now.";
    public const string VolumeInvalid = "Please use a value between 0 and 100.";
}
