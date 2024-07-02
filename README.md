<br/>
<p align="center">
  <h3 align="center">AzzyBot</h3>
  
  <p align="center">
    Kind of music bot for discord, written in C# and with DSharpPlus. This bot is dedicated for the use with <a href="https://github.com/AzuraCast/AzuraCast">AzuraCast</a> and does not work at it's fully glory without it. 
    <br/>
    <br/>
    <a href="https://github.com/Sella-GH/AzzyBot/wiki"><strong>Explore the docs »</strong></a>
    <br/>
    <br/>
    <a href="https://github.com/Sella-GH/AzzyBot/issues">Report Bug</a>
    .
    <a href="https://github.com/Sella-GH/AzzyBot/issues">Request Feature</a>
  </p>
</p>

[![Downloads](https://img.shields.io/github/downloads/Sella-GH/AzzyBot/total)](https://github.com/Sella-GH/AzzyBot/releases)
[![DownloadsLatest](https://img.shields.io/github/downloads/Sella-GH/AzzyBot/latest/total)](https://github.com/Sella-GH/AzzyBot/releases/latest)
[![DockerPulls](https://img.shields.io/docker/pulls/sellagh/azzybot)](https://hub.docker.com/r/sellagh/azzybot)
[![Contributors](https://img.shields.io/github/contributors/Sella-GH/AzzyBot?color=dark-green)](https://github.com/Sella-GH/AzzyBot/graphs/contributors)
[![Issues](https://img.shields.io/github/issues/Sella-GH/AzzyBot)](https://github.com/Sella-GH/AzzyBot/issues)
[![PullRequests](https://img.shields.io/github/issues-pr/Sella-GH/AzzyBot)](https://github.com/Sella-GH/AzzyBot/pulls)

## Table Of Contents

* [About the Project](#about-the-project)
* [Built With](#built-with)
* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
* [Roadmap](#roadmap)
* [Contributing](#contributing)
* [License](#license)
* [Authors](#authors)
* [Acknowledgements](#acknowledgements)

## About The Project

Since my previous project "needed" a discord bot solution to comfort the opening and closing of a so called "club" - I built a discord bot to ensure the best comfort while using it.

The bot contains of the following features:
- A modular approach on the activating/disabling of features
- Help command to see all features
- Integrated updater (more or less functional)
- Resource usage of AzuraCast and AzzyBot displayed in embeds
- Checks if files were changed, AzuraCast is offline or automatic "closing" if no listeners are reported on the station
- Playlist export
- Local cache of music metadata
- Switching playlists
- Get song history of the last 14 days
- Get songs in playlist
- Currently played song
- Song requests
- Favorite songs (manually)
- Opening and closing a "club" (a station) also includes statistics for the "opening"

## Built With
### Software
* [.NET 8](https://github.com/dotnet/runtime)
* [Visual Studio 2022 Community](https://visualstudio.microsoft.com/de/downloads)
* [Lavalink](https://github.com/lavalink-devs/Lavalink)
* [Docker](https://docker.com/)
* [Docker Compose](https://docker.com/)

### Dependancies
* [CsvHelper](https://github.com/JoshClose/CsvHelper)
* [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
* [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET)
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
* [Roslynator](https://github.com/dotnet/roslynator)

## Getting Started

To get a local copy up and running follow these simple example steps.

### Prerequisites

- Operating systems
  - Debian or Ubuntu
  - Windows (not all features available)
  - ARM64 or x64 (no ARM32 or x86 support!)
- The latest release from [here](https://github.com/Sella-GH/AzzyBot/releases)
- Docker, if you want to use the image (optional)

### Installation

Follow the instructions inside the [Wiki](https://github.com/Sella-GH/AzzyBot/wiki).

## Roadmap

See the [open issues](https://github.com/Sella-GH/AzzyBot/issues) for a list of proposed features (and known issues).

## Contributing

Contributions are what make the open source community such an amazing place to be learn, inspire, and create. Any contributions you make are **greatly appreciated**.
* If you have suggestions for adding or removing projects, feel free to [open an issue](https://github.com/Sella-GH/AzzyBot/issues/new) to discuss it, or directly create a pull request after you edit the *README.md* file with necessary changes.
* Please make sure you check your spelling and grammar.
* Create individual PR for each suggestion.
* Please also read through the [Code Of Conduct](https://github.com/Sella-GH/AzzyBot/blob/main/CODE_OF_CONDUCT.md) before posting your first idea as well.

### Creating A Pull Request

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the AGPL-3.0 License. See [LICENSE](https://github.com/Sella-GH/AzzyBot/blob/main/LICENSE) for more information.

## Authors

* [Sella-GH](https://github.com/Sella-GH)

## Acknowledgements

* CurtWoodman
