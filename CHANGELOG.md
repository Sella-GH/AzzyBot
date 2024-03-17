### 1.8.0
#### General
- Now dependant on [Lavalink4NET.DSharpPlus](https://github.com/angelobreuer/Lavalink4NET) too
- Now dependant on OpenJDK Runtime 17 too
- Now includes [Lavalink](https://github.com/lavalink-devs/Lavalink) inside the installation directory (size got bumped up by 60 MB)

#### Additions
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