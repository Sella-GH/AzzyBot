## 1.10.0
### Breaking Changes
- `StringsMusicStreaming.json` was changed

### Dependancies
- Updated [CsvHelper](https://github.com/JoshClose/CsvHelper) to 32.0.1

### General
- `core info azzy` was renamed to `core info`
- `core ping azzy` was renamed to `core ping`
- Internal change from `Newtonsoft.Json` to `System.Text.Json` (probably resulting in performance improvements)

### Improvements
- AzuraCast update notifications aligned to the timeframe of Core update notifications
- AzuraCast checks now fire before the command is executed
  - This means the users get a ephemeral (not visible for everyone) notification if something is not correct (e.g. server down, api key not working, etc)
  - You continue to get the normal error messages if something goes wrong during the command execution

### Fixes
- 1-Min-Load not showing when it's 0 in `core ping`
- Update notifications not firing properly
- Lavalink not being able to load the current version of the Dependancy [Lyrics.Java](https://github.com/DuncteBot/java-timed-lyrics)

## 1.9.1
### Dependancies
- Updated [CsvHelper](https://github.com/JoshClose/CsvHelper) to 32.0.0
- Updated [Lyrics.Java](https://github.com/DuncteBot/java-timed-lyrics) to 1.6.4

### Fixes
- Bot not starting because `appsettings.json` got wrong values - Thanks [R3dlessX](https://github.com/R3dlessX)!
- `azuracast export-playlist` not working because of wrong path

## 1.9.0
### Breaking changes
- `appsettings.json` is now located in the `Settings` folder
- `appsettings.json` was restructured and extended
- Some strings in the following files were updated
  - `StringsAzuraCast.json`
  - `StringsClubManagement.json`
  - `StringsCore.json`
  - `StringsMusicStreaming.json`

### General
- The updater is now included instead of an own application and only provides notifications. You have to update the bot manually
- Now available as a docker image too!
  - Follow the instructions in the [wiki](https://github.com/Sella-GH/AzzyBot/wiki/Docker-Install-Instructions) if you want to get it up and running
  - The bot is still available as a base arm64 or x64 dedicated executable if you prefer this one

### Additions
- Messages if the music streaming module is not used for the specific amount of time OR if no users are in the voice channel anymore
- Support for AzuraCast HLS streams
- `core info azzy` and `core ping azzy` now include more details about the bot and it's underlying system
- The exception embed now includes a callout to report the bug
- `AzuraApiKey` is not required anymore
- A check if the given `AzuraApiKey` is valid
  - This requires you to give the bot API key access to the "Station Media" permission
- The use of the MusicStreaming module without an enabled AzuraCast Web Proxy
- String checking in `Customization` to prevent empty string and NullReferenceExceptions
- A few new settings:
  - Core/Updater
    - `DisplayChangelog`: bool
	- `DisplayInstructions`: bool
	- `UpdateCheckInterval`: int
	- `UpdateMessageChannelId`: int
  - AzuarCast/AutomaticChecks
    - `FileChanges` (before AutomaticFileChangeCheck) : bool
    - `ServerPing` (before AutomaticServerPing) : bool
    - `Updates` (before AutomaticUpdateCheck) : bool
    - `UpdatesShowChangelog`: bool
  - AzuraCast
    - `ShowPlaylistsInNowPlaying`: bool
  - MusicStreaming
    - `StreamingPort`: int

### Removements
- `config bot-restart` command
- Core Settings
  - `LogLevel`: int - Replaced by setting the log based on the exec file
 
### Improvements
- CI Updates for better processability
- Some settings now have a default value
- More and better logging in case something goes wrong
- Useless checks removed
- Better check for lavalink stuff implemented
- The commit and compile date infos are now included in a AzzyBot.json file
- `appsettings.json` now supports an ip address as value for the `AzuraApiUrl` setting
- Reworked the way how the bot checks if the server is reachable
- Caching improvements of settings and string files
- Code and reliability improvements

### Fixes
- IPv4 only issues with api key related requests

## 1.8.0
### General
- Now dependant on [Lavalink4NET.DSharpPlus](https://github.com/angelobreuer/Lavalink4NET) too
- Now dependant on OpenJDK Runtime 17 too
- Now includes [Lavalink](https://github.com/lavalink-devs/Lavalink) inside the installation directory (size got bumped up by 60 MB)

### Additions
- New logo to GitHub repository
- New module `MusicStreaming` with new commands (more info in the Wiki)
  - `player disconnect` = Disconnect the bot from your voice channel
  - `player join` = Joins the bot into your voice channel
  - `player set-volume` = Changes the volume of the player
  - `player show-lyrics` = Shows you the lyrics of the current played song
  - `player start` = Starts the music stream into your voice channel
  - `player stop` = Stops the music stream from playing
  
### Improvements
- Code cleanup
- CI Updates for better processability

### Fixes
- `core info azzy` and `core ping azzy` can now be used on windows too
  - `core ping azzy` has way less information than on linux because of the insufficient rights of a regular user account

## 1.7.1
Initial public release with GitHub Actions
