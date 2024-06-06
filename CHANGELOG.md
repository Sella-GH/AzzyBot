## 2.0.0-preview2
### General
- The integrated updater is now able to identify preview versions
- The `AzuraCast` module is finally added!
  - Now you can finally use the bot again and work with it

### Additions
- New commands:
  - `music nowplaying` - Shows the currently played stuff from the choosen station

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
