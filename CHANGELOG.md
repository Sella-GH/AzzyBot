## 1.10.0
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
