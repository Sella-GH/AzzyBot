## 1.9.0
### Breaking changes
- `appsettings.json` is now located in the `Settings` folder
- Some strings in the following files were updated
  - `StringsAzuraCast`
  - `StringsClubManagement`

### General
- Now available as a docker image too!
  - Follow the instructions in the wiki if you want to get it up and running
  - The bot is still available as a base x64 or arm64 dedicated executable if you prefer this

### Additions
- `appsettings.json` now supports an ip address as value for the `AzuraApiUrl` setting
- Messages added if the music streaming plugin is not used for the specific amount of time OR if no users are in the voice channel anymore
 
### Improvements
- CI Updates for better processability
- Some settings now have a default value
- More logging in case something goes wrong
- Useless checks removed
- Code and reliability improvements
