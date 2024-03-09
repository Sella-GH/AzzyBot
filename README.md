<br/>
<p align="center">
  <h3 align="center">AzzyBot</h3>

  <p align="center">
    General purpose discord bot, written in C# and with DSharpPlus, dedicated for the use with AzuraCast.
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

![Downloads](https://img.shields.io/github/downloads/Sella-GH/AzzyBot/total) ![Contributors](https://img.shields.io/github/contributors/Sella-GH/AzzyBot?color=dark-green) ![Issues](https://img.shields.io/github/issues/Sella-GH/AzzyBot)

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

* [.NET 8](https://github.com/dotnet/runtime)
* [Visual Studio 2022 Community](https://visualstudio.microsoft.com/de/downloads/)
* [CsvHelper 31.0.2](https://github.com/JoshClose/CsvHelper)
* [DSharpPlus 4.4.6](https://github.com/DSharpPlus/DSharpPlus)
* [Roslynator 4.11](https://github.com/dotnet/roslynator)

## Getting Started

To get a local copy up and running follow these simple example steps.

### Prerequisites

- Operating systems
  - Debian or Ubuntu
  - Windows (not all features available)
  - ARM64 or x64 (no ARM32 or x86 support!)
- The latest release from [here](https://github.com/Sella-GH/AzzyBot/releases)

### Installation

Follow the instructions inside the Wiki.

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

* [CurtWoodman](https://github.com/CurtWoodman)
