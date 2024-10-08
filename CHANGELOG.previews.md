## 2.0.0-preview9
This is a release candidate. If no major bugs are found, this will be the final release.

### General
- Greatly overhauled the missing permission handling
- Permission check messages are now including the new wiki [link](https://github.com/Sella-GH/AzzyBot/wiki/AzuraCast-API-Key-required-permissions)
- Updated the permission check uris

### Dependencies
- New dependency on [System.Linq.Async](https://github.com/dotnet/reactive) version 6.0.1

### Improvements
- Improvements were made regarding receiving of song requests
- Consolidated some code to reduce bloat
- Code performance improvements

### Fixes
- `player join`, `player play` and `player play-mount` are working again when the user isn't classified as "in a voice channel" by discord

## 2.0.0-preview8 - 2024-08-09
### General
- Failsafes added to prevent the bot from crashing
- Refactored the code to comply with the newest dependency versions

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02350
- Replaced [Lavalink4NET.DSharpPlus](https://github.com/angelobreuer/Lavalink4NET) with [Lavalink4NET.DSharpPlus.Nightly](https://github.com/angelobreuer/Lavalink4NET) version 4.0.20

### Additions
- New command `core force-channel-permissions-check` which checks all discord channels if the bot has the correct permissions
- Every 15 minutes the bot checks if it has the correct permissions in every ever-set channel

### Improvements
- Consolidated some code to make it more readable and maintainable (and hopefully faster)
- The bot checks now if the voice channel to enter is visible and joinable for it

## 2.0.0-preview7 - 2024-08-06
### Breaking Changes
- Your Database needs a complete reset, please DROP it and start from scratch
  - This is probably the last time it needs to be dropped by now

### General
- Reworked the way how the background tasks are handled and extended the queue up to 1024 items
- Renamed `player play` to `player play-mount` and adjusted the description
- Streaming music from `SoundCloud` is now possible over the integrated music streaming module!
- More preparations for the final release of the bot

### Dependencies
- Updated [EntityFrameworkCore.Exceptions.PostgreSQL](https://github.com/Giorgi/EntityFramework.Exceptions) to version 8.1.3

### Additions
- New Commands
  - `player play` to select the provider and provide a url
  - `player pause` to pause the player
  - `player resume` to resume the player
  - `player queue-clear` to clear the whole queue or only one song
  - `player skip` to skip a song
  - `player history` to view the song history
  - `player queue` to view the song queue

### Improvements
- Multiple song requests occuring nearly the same time should now be accepted correctly
- Promoted the last missing timer task to be a background task
- `player play-mount` now shows the name of the station which is played
- `music search-song` can now only be used in the music request channel
- Station file changes are now posted in the AzuraCast Instance notification channel (just like the others)
- Moved some code around the libs to better separate it
- Cleaned up the workflows
- Updated the constant link to the updater page

### Fixes
- `azuracast force-update-check` works again

## 2.0.0-preview6 - 2024-08-01
### General
- Performance improvements all over the board. The bot should now be faster and more reliable
- Reworked the way how strings and messages displayed to the user are handled

### Improvements
- The `music upload-files` command checks the uploaded file if it has valid performer and title tag

## 2.0.0-preview5 - 2024-07-29
### Breaking Changes
- Your Database needs a complete reset, please DROP it and start from scratch
  - You'll likely experience this a few more times until the final release
  - No, I won't provide migration scripts for preview versions unless I'm fully confident that the database structure is somewhat final

### General
- Reworked the database interactions again so it should be better, faster and smarter now
- Reworked the internals to make it better maintainable and smoother
- Renamed `azuracast upload-files` to `music upload-files`
- Prepared the bot metadata for the final release

### Additions
- The `MusicStreaming` module is back
  - If the station is stopped while someone is listening they get disconnected first
  - You can stream your stations mount points again
- New command `admin view-logs` to view the system logs of AzzyBot

### Removements
- Removed the `MountPoint` entity of the database
- Removed the `Name` property in the station config

### Improvements
- `music now-playing` shows the station name now
- Improved some embed structuring

### Fixes
- `config modify-azuracast-station` works again
- Logging of 404 errors in the AzuraCast API is gone when starting the station and Liquidsoap is not ready yet
- `azuracast update-instance` works again
- `music now-playing` works again when the station is offline

## 2.0.0-preview4.1 - 2024-07-21
### Fixes
- The update instructions for docker are now correct
- The `core help` single command is now working correctly again

## 2.0.0-preview4 - 2024-07-21
### Breaking Changes
- Your Database needs a complete reset, please DROP it and start from scratch
  - You'll likely experience this a few more times until the final release
  - No, I won't provide migration scripts for preview versions unless I'm fully confident that the database structure is somewhat final

### General
- Removed not needed internal events, meaning the performance should be *slightly* better
- Restructured the whole codebase to make it more readable and maintainable
- Refactored the database Entities a bit so they are separated into more tables
- Greatly refactored the database actions in general (again)

### Additions
- You can now upload files to your AzuraCast Station using `azuracast upload-files`
  - You are able to specify a specific channel where people are able to upload the files only

### Removements
- The `debug-server` command group including all commands

### Improvements
- The local station cache file is now also deleted when the station or the instance is deleted
- If you have activated "Always Write Playlists to Liquidsoap", the bot will wait for it before the station is completely started
- A timeout of 30s after each song skip has been added to prevent the bot from skipping songs too fast
- Removed useless code
- API permission checks ignore disabled features of the station

### Fixes
- Permission checks are now really working correctly
- The `admin send-bot-wide-message` command was corrected so it's finally future proof

## 2.0.0-preview3.1 - 2024-07-14
### General
- Optimized the building process for better code performance
- A new environment variable was added to skip the waiting time for the database at startup

### Additions
- More logging messages in case something goes absurdly wrong

### Improvements
- Ensures consistency over all commands regarding variable names
- Errors now mention people and groups correctly

### Fixes
- Permission checks of AzuraCast commands not working
- Roles not showing when checks fail

## 2.0.0-preview3 - 2024-07-14
### Breaking Changes
- The database has switched from MariaDB to PostgreSQL
  - You already know what that means (DATA DELETION)
  - Please clean your docker system too after you shut down the bot
    - docker system prune -a -f --volumes
    - docker volume prune -a -f

### General
- Reworked the whole background database structure to make it easier, faster and less ressource-consuming
- Changing the database encryption key is now possible
  - You have to add a new value to "NewEncryptionKey" in your settings.json
  - The bot then starts to reencrypt every encrypted value with your new key
  - PLEASE MAKE A BACKUP OF YOUR DATABASE BEFORE!
  - Use [pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html) as a helper tool

### Additions
- `azuracast get-system-logs` You are now able to view system logs of your instance
- `admin get-joined-server` You are now able to see all servers in which the bot is
- `admin remove-joined-serber` You are now able to remove the bot from certain servers
- `admin send-bot-wide-message` You are now able to send a message to all server owners and channels (if they specified it)
  - **Please, for the sake of anything, use this thing only when there is something extremly important to tell everyone**!
  - *No, it's not your birthday, marriage or something else. It's when the bot goes down for maintenance or similar stuff*
- New notifications when the bot joins or leaves servers

### Improvements
- AzuraCast command checks are now reviewed by priority
  - If the module is not activated the answer will be shouted first now
- Changed some "True/False" boolean choices to "Enable/Disable" or "Yes/No"
- Removed debug responses from answers
- Removed the started/stopped additions in some autocompletes
- All autocompletes are now searchable

### Fixes
- `azuracast update-instace` says the station is already up-to-date but it's not
- `admin debug-servers remove-servers` autocomplete not working
- `config get-settings` looks now correct when displaying roles from AzuraCast and Core
- Typo in the parameter description `format` of `azuracast export-playlists`
- Autocomplete of `core help` not working

## 2.0.0-preview2 - 2024-07-06
### Breaking Changes
- Your Database needs a complete reset, please DROP it and start from scratch
  - You'll likely experience this a few more times until the final release
  - No, I won't provide migration scripts for preview versions unless I'm fully confident that the database structure is somewhat final

### General
- Greatly reworked the way how background checks, features and other stuff is handled
- The integrated updater is now able to identify preview versions
- Unified the config command names of the AzuraCast stuff
  - Yes, this means that the command names have changed
  - Also the whole background check configuration shifted from one database table to others

### Additions
- The `AzuraCast` module is finally added!
  - Now you can use the bot again and work with it like before (just in better)
  - Implemented new commands, new background checks and a lot of API stuff
- AzuraCast Admin Api Key to AzuraCast configuration added
  - This is a required field if you want to set up AzuraCast in the bot. Ensure it has the `view` permission for the admin panel inside AzuraCast
- Setting to control the logfile cleaning after specified days (default 7)
- Checks if modules are activated or not before excuting commands
- New database settings for servers to modify in `core`

### Improvements
- AzuraCast Station Api Key in AzuraCastStation configuration is now optional
  - Because the Admin Api Key is required, the station one is now optional. However if you do not add a station specific key the admin key will be used!
- File logging now uses a shared static StreamWriter
  - This should improve the performance and stability of logging
- The `core help` command is now ready for the future
- You can now specify your timezone (TZ=) inside the docker compose file

### Fixes
- Guild Database Entity was not updated when AzuraCast was activated or disabled
- Some autocomplete exceptions

## 2.0.0-preview1.1 - 2024-06-06
### General
- Your Database needs a complete reset, please DROP it and start from scratch.

### Additions
- File Logging to folder `Logs`
  - There is one log file per day

### Improvements
- Check if database is online before trying to connect
- Detection of operating systems in the hardware embed
- Complete rework of the logic behind `core help` command
- Code and reliability improvements

### Fixes
- Database does not save objects when they are modified
- Debug Servers not showing inside the AutoComplete, affected commands:
  - `admin debug-servers add-server`
  - `admin debug-servers remove-server`

## 2.0.0-preview1 - 2024-06-01
### General
- Complete rewrite of the bot using newest technologies
- Included a database to store your settings and other important data
- New commands to manage your bot and settings inside a database
- Support for multiple servers, multiple stations and multiple users

Please note that this is a preview version and not all features are implemented yet. Some features may not work as expected or may not work at all.
If you find any bugs or have any suggestions, please let me know by creating an [issue](https://github.com/Sella-GH/AzzyBot/issues/new/choose) on GitHub.
