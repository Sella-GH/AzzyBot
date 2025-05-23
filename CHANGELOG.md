## 2.4.0
### Breaking Changes
- This version is only compatible with AzuraCast 0.21.0 and upwards!

### General
- Changed docker images to use Ubuntu 24.04 instead of Debian 12
  - This change was made based on referring that Ubuntu seems to be the [preferred](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu) container OS for .NET 
  - Also we got a quite nice size improvement about up to 15 MB

### Improvements
- We now use Zstd compression for DSharpPlus resulting in better compression, performance and less memory usage
- Adjusted to API changes made in AzuraCast Commit PR [#7713](https://github.com/AzuraCast/AzuraCast/pull/7713)

### Development
- Moved some stuff out of dockerfiles to GitHub workflow
- Docker dev versions show stats again

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02512

## 2.3.1 - 2025-03-03
### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02461
- Updated [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) to version 4.0.27
- Updated [Microsoft.EntityFrameworkCore.Tools](https://github.com/dotnet/efcore) to version 9.0.2
- Updated [NCronJob](https://github.com/NCronJob-Dev/NCronJob) to version 4.3.4
- Updated [Npgsql](https://github.com/npgsql/efcore.pg) to version 9.0.3
- Updated [Npgsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg) to version 9.0.4
- Updated [Roslynator](https://github.com/dotnet/roslynator) to version 4.13.1
- Updated [SonarAnalyzer.CSharp](https://github.com/SonarSource/sonar-dotnet) to version 10.7.0.110445

### Fixes
- Checking if a song was requested already now works exactly as wanted
- Options validation now works correctly and doesn't throw exceptions anymore
- `music nowplaying` works again when you stream over Icecast instead of the integrated AzuraCast stream feature

## 2.3.0 - 2025-01-26
### General
- A new docker compose variable "LOG_LEVEL"
  - This variable allows you to set the log level of the bot
  - FORCE_DEBUG and FORCE_TRACE are now deprecated
  - Please adjust your docker-compose file accordingly

### Improvements
- Added some more logging messages
- Removed some unneeded and wrongly placed logging messages
- The AzuraCast Settings embed is now shown even when the instance is offline
  - The AzuraCast Stations embed however continues to be not shown
- When the AzuraCast instance has a self-signed SSL certificate Azzy will now warn about it
- Logfile cleaning gets triggered at startup now and works again
- Speed up `core stats info` by about 44 times by using a faster approach
- The connection to the database is now more resilient against issues with the data
- Centralized the notification of exceptions to the main server

### Fixes
- The dm addition if the global bot message was sent to a user directly is now displayed correctly

### Development
- We're now using the new ARM64 GitHub provided runners for Docker images
  - This results in a faster build time and more compact actions workflow
- Removed all manually checked references if the environment is dev/docker
  - The bot is now conditionally compiled based on preprocessor directives
- Refactored DbContext stuff to use the new `PooledDbContextFactory` pattern
  - This should finally fix the concurrency issues and make it work as intended
- Posting files to discord works now while they're being in use on other platforms than Linux

## 2.2.5 - 2025-01-25
### Dependencies
- Updated [NCronJob](https://github.com/NCronJob-Dev/NCronJob) to version 4.3.1

### Improvements
- The initial welcome message is now an embed with more info

### Fixes
- Null reference exception when the bot should join a voice channel but the requesting user isn't in one

## 2.2.4 - 2025-01-21
### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02450
- Updated [NCronJob](https://github.com/NCronJob-Dev/NCronJob) to version 4.2.0
- Updated [Npgsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg) to version 9.0.3

## 2.2.3 - 2025-01-15
### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02448
- Updated [Microsoft.EntityFrameworkCore.Tools](https://github.com/dotnet/efcore) to version 9.0.1
- Updated [Microsoft.Extensions.Hosting](https://github.com/dotnet/runtime) to version 9.0.1

## 2.2.2 - 2025-01-07
### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02445

### Fixes
- There won't be any update checks or database cleanups any 15 minutes anymore
- The GitHub update url to the new version now really points to the release and not the API
- File logging now shows the EventId properly instead of the event name to align with the console logging
- You can now correctly force the check to determine if AzuraCast is offline
- Some spelling mistakes were corrected
- Automatic checks getting executed correctly now when their time has come

## 2.2.1 - 2025-01-05
### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02445
- Updated [NCronJob](https://github.com/NCronJob-Dev/NCronJob) to version 4.1.0

## 2.2.0 - 2024-12-23
### BREAKING CHANGES
- The settings file structure changed and will require a migration!

### General
- Upon invitation the bot will now require acceptance of the Privacy Policy and Terms Of Service

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02439
- Updated [Roslynator](https://github.com/dotnet/roslynator) to version 4.12.10
- Updated [SonarAnalyzer.CSharp](https://github.com/SonarSource/sonar-dotnet) to version 10.4.0.108396

### Additions
#### New parameters
- `player play` and `player play-mount` gained an additional optional parameter `volume` which allows to set the volume of the player at startup
  - The volume can be set between 0 and 100
  - The default volume is 100
  - This value is only respected if the player is not playing anything

#### New commands
- `admin reset-legals` command added
  - This command is only for administrators of the bot (the main server)
  - It resets the accepted legals for every guild and requires them to reaccept
- `dj add-internal-song-request` command added
  - This command allows you to add a song quietly into the AutoDj queue
  - These kind of song requests are not logged inside AzuraCast and should be used with caution!
  - The bot however logs these kind of requests
- `legals accept-legals` command added
  - This command allows you to accept our legal terms and policies

### Improvements
- Server info embed for admins was slightly improved and fixed
- `config reset-settings` now shows a confirmation message before resetting the settings
- The exception embed was slightly improved with more details
- Improvements regarding information security
- The bot respects in all cases now when the user decides to disable local file caching
  - This means that it's not possible to retrieve information about uploaded files when the whole instance is offline
- The hardware embed now includes the amount of memory which the bot uses
- The stats embed was restructured and now includes the legal stuff (License, Privacy Policy, Terms Of Service)
- Updated the invite link so the bot now needs the "Embed Links" permission too

### Fixes
- The 15 minute cron job won't error anymore when the instance is offline
- The settings embed now gets created again when the instance is offline

### Development
- Refactored the code to use `System.Text.Json` source generator and removed `System.Reflection` calls
- Refactored the settings code to use the Options Pattern with source generation validation
- Debug code was excluded from compiling in release mode
- `.editorconfig` now default to warnings

## 2.1.2 - 2024-12-15
### Dependencies
- Updated [Npgsql](https://github.com/npgsql/npgsql) to version 9.0.2
- Updated [Npgsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg) to version 9.0.2

### Fixes
- `music get-song-history` works again
- The initial online check after creating an instance works again

## 2.1.1 - 2024-11-23
### Improvements
- The AzuraCast updater works fine now if a rolling release instance is expected to update to a stable release

## 2.1.0 - 2024-11-22
### General
- Updated to .NET 9 including all dependencies
- Dockerfile improvements to improve image size

### Dependencies
- Added [NCronJob](https://github.com/NCronJob-Dev/NCronJob) in version 3.3.8
- Added [NReco.Logging.File](https://github.com/nreco/logging) in version 1.2.2
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02423
- Updated [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) to version 4.0.26-preview.4

### Additions
- Added a new hidden environment variable "FORCE_TRACE" to docker-compose which forces the app to trace log (not recommended for production)
  - This only works in dev mode
- Added a new environment variable "LOG_RETENTION_DAYS" to docker-compose which defines how many days the logs should be kept
  - Default is 7 days
- `config get-settings` now shows an additional value in the "station" section which shows how many times songs were requested through the bot

### Improvements
- Exception embeds now produce JSON output instead of a StackTrace.log file
- Startup logging now shows the .NET version too
- Reworked the whole file logging system
- `admin send-bot-wide-message` now allows line breaks using `\n` and includes a message if it's sent directly to the server owner
- QuickJit for loops was activated to improve performance of the bot
- Reworked background tasks to use NCronJob instead of a custom implementation
- `azuracast force-cache-refresh` parameter `station` is now optional
  - If there is no value given it will refresh the cache of all configured stations
- `azuracast stop-station` now sends a message to the connected voice channels if it's playing and was stopped
- Various enhancements were made while displaying mounts inside the autocomplete

### Fixes
- The AzuraCast station cache refresh works again
- `music now-playing` now shows the streamer artwork if a streamer is playing

## 2.0.13 - 2024-11-14
### Dependencies
- Updated [Microsoft.Extensions.Caching.Memory](https://github.com/dotnet/runtime) to version 9.0.0
- Updated [Microsoft.Extensions.Hosting](https://github.com/dotnet/runtime) to version 9.0.0
- Updated [Microsoft.EntityFrameworkCore.Tools](https://github.com/dotnet/efcore) to version 8.0.11
- Updated [System.Text.Json](https://github.com/dotnet/runtime) to version 9.0.0

### Improvements
- Sets the Service Lifetime of database services to `Singleton` again with the exception of the `AzzyBotDbContext` which is now `Scoped`

## 2.0.12 - 2024-11-12
### Improvements
- Removed all parameters from database relevant methods which are intended to pass `DateTimeOffset.UtcNow` values and replaced them with direct calls inside the respecting method
  - This *is* the final fix for the timer issues

## 2.0.11 - 2024-11-09
### Improvements
- Moved from `DateTime` to `DateTimeOffset` to prevent issues with time zones
  - This should now hopefully fix all the issues occurring with the timer

## 2.0.10 - 2024-11-07
### Improvements
- Reworked how the background check system works to fix issues

## 2.0.9 - 2024-11-03
### Fixes
- The bot now only checks each 12h if the discord permissions are set to prevent hitting discord rate limits

## 2.0.8 - 2024-10-27
### Dependencies
- Updated [Npgsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg) to version 8.0.10
- Updated [Roslynator](https://github.com/dotnet/roslynator) to version 4.12.9

### Improvements
- Set the database context and related services to transient to prevent issues with the database connection

## 2.0.7 - 2024-10-25
### Dependencies
- Added and updated [Microsoft.Extensions.Caching.Memory](https://github.com/dotnet/runtime) to version 8.0.1 to fix vulnerabilities

### Improvements
- `core help` now shows an additional embed telling the user how to correctly set up the bot

## 2.0.6 - 2024-10-13
### General
- Recreated docker images because of vulnerabilities

### Dependencies
- Now dependent on [Npgsql](https://github.com/npgsql/npgsql) in version 8.0.5
- Updated [Microsoft.Extensions.Hosting](https://github.com/dotnet/runtime) to version 8.0.1
- Updated [Microsoft.EntityFrameworkCore.Tools](https://github.com/dotnet/efcore) to version 8.0.10
- Updated [Npgsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg) to version 8.0.8
- Updated [Roslynator](https://github.com/dotnet/roslynator) to version 4.12.8
- Updated [System.Text.Json](https://github.com/dotnet/runtime) to version 8.0.5

### Improvements
- Tiny code changes

## 2.0.5 - 2024-09-24
### Dependencies
- Updated [Roslynator](https://github.com/dotnet/roslynator) to version 4.12.6

### Fixes
- There is no database error anymore when the bot is added to a guild. [#181](https://github.com/Sella-GH/AzzyBot/issues/181)

## 2.0.4 - 2024-09-21
### Dependencies
- Updated [Roslynator](https://github.com/dotnet/roslynator) to version 4.12.5

### Fixes
- AzuraCast Central update responses are now more reliable and don't throw exceptions anymore

## 2.0.3 - 2024-09-10
### General
- Recreation of docker images because of vulnerabilities

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02361

## 2.0.2 - 2024-09-03
### General
- Recreation of docker images because of vulnerabilities

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02360
- Updated [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) to version 4.0.25

## 2.0.1 - 2024-08-18
### General
- `admin change-bot-status` requires activity, status and doing to be set now

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02356
- Updated [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) to version 4.0.21

### Improvements
- Greatly refactored the handling of DSharpPlus related stuff
- The usual connection error message of updating AzuraCast should not be shown anymore
- `core stats hardware` now shows the latency to discord
- More internal restructuring to make the code more functional
- File logging now rotates the logs daily or when exceeding 24.9 MB file size
- Adjusted maximum upload size of `music upload-files` to 49.9 MB
- Leftover servers in database are now removed every day

### Fixes
- MusicStreaming commands exit correctly now when there is an error
- Command error handling works flawlessly again
- `admin get-joined-server` works again (and shows the server defined in `AzzyBotSettingsDocker.json` too)
- `player now-playing` doesn't throws an error when it's used while playing a mount point

## 2.0.0 - 2024-08-11
### General
- Complete rewrite of the bot using newest technologies
- Includes a database to store your settings and other important data
- New commands to manage your bot and settings
- Support for multiple servers, multiple stations and multiple users

Read the [docs](https://github.com/Sella-GH/AzzyBot/wiki/) if you need help setting the bot up.
