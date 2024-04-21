## 1.9.0
### Breaking changes
- `appsettings.json` is now located in the `Settings` folder
- Some strings in the following files were updated
  - `StringsAzuraCast.json`
  - `StringsClubManagement.json`
  - `StringsCore.json`
  - `StringsMusicStreaming.json`

### General
- The updater is now included instead of an own application and only provides notifications. You have to update the bot manually
- Now available as a docker image too!
  - Follow the instructions in the wiki if you want to get it up and running
  - The bot is still available as a base arm64 or x64 dedicated executable if you prefer this one

### Additions
- Messages if the music streaming module is not used for the specific amount of time OR if no users are in the voice channel anymore
- Support for AzuraCast HLS streams
- `core info azzy` and `core ping azzy` now include more details about the bot and it's underlying system
- The exception embed now includes a callout to report the bug
- `AzuraApiKey` is not required anymore
- A check if the given `AzuraApiKey` is valid
  - This requires you to give the bot API key access to the "Station Media" permission
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
    - `UpdatesShowChangelog` : bool
  - AzuraCast
    - `ShowPlaylistsInNowPlaying` : bool

### Removements
- `config bot-restart` command
 
### Improvements
- CI Updates for better processability
- Some settings now have a default value
- More and better logging in case something goes wrong
- Useless checks removed
- Better check for lavalink stuff implemented
- The commit and compile date infos are now included in a AzzyBot.json file
- `appsettings.json` now supports an ip address as value for the `AzuraApiUrl` setting
- Caching improvements of settings and string files
- Code and reliability improvements

### Fixes
- IPv4 only issues with api key related requests
