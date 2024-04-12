## 1.9.0
### Breaking changes
- `appsettings.json` is now located in the `Settings` folder
- Some strings in the following files were updated
  - `StringsAzuraCast.json`
  - `StringsClubManagement.json`
  - `StringsCore.json`

### General
- The updater is now included instead of an own application and only provides notifications. You have to update the bot manually
- Now available as a docker image too!
  - Follow the instructions in the wiki if you want to get it up and running
  - The bot is still available as a base arm64 or x64 dedicated executable if you prefer this one

### Additions
- Messages added if the music streaming plugin is not used for the specific amount of time OR if no users are in the voice channel anymore
- Support for AzuraCast HLS streams
- `core info azzy` and `core ping azzy` now include more details about the bot
- `appsettings.json` now supports an ip address as value for the `AzuraApiUrl` setting
- `AzuraApiKey` is not required anymore
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
- More logging in case something goes wrong
- Useless checks removed
- Better check for lavalink stuff implemented
- The exception embed now includes a callout to report the bug
- The commit and compile date infos are now included in a AzzyBot.json file
- Code and reliability improvements

### Fixes
- IPv4 only issues with api key related requests
