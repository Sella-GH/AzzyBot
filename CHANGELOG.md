## 2.0.0-preview2
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

### Fixes
- Guild Database Entity was not updated when AzuraCast was activated or disabled
- Some autocomplete exceptions

## 2.0.0-preview1.1
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

## 2.0.0-preview1
### General
- Complete rewrite of the bot using newest technologies
- Included a database to store your settings and other important data
- New commands to manage your bot and settings inside a database
- Support for multiple servers, multiple stations and multiple users

Please note that this is a preview version and not all features are implemented yet. Some features may not work as expected or may not work at all.
If you find any bugs or have any suggestions, please let me know by creating an [issue](https://github.com/Sella-GH/AzzyBot/issues/new/choose) on GitHub.
