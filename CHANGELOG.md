## 2.1.0
### General
- Updated to .NET 9 including all dependencies
- Dockerfile improvements to improve image size
### Dependencies
- Added [NReco.Logging.File](https://github.com/nreco/logging) in version 1.2.1
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02394
- Updated [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) to version 4.0.26-preview.4

### Additions
- Added a new hidden environment variable "FORCE_TRACE" to docker-compose which forces the app to trace log (not recommended for production)
  - This only works in dev mode
- Added a new environment variable "LOG_RETENTION_DAYS" to docker-compose which defines how many days the logs should be kept
  - Default is 7 days

### Improvements
- Exception embeds now produce json output instead of a stacktrace.log file
- Startup logging now shows the .NET version too
- Reworked the whole file logging system
- `admin send-bot-wide-message` now allows linebreaks using `\n` and includes a message if it's sent directly to the server owner

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
- Recreation of docker images because of vulnerabilites

### Dependencies
- Updated [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) to version 5.0.0-nightly-02361

## 2.0.2 - 2024-09-03
### General
- Recreation of docker images because of vulnerabilites

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
