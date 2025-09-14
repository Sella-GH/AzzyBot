# AzzyBot Security Policy

## Supported Versions

We expect that all users of the software, if they are interested in taking advantage of the latest bug and security fixes, remain up-to-date with the latest version of the software. Older versions of the software are not officially supported or maintained.

| Version | Supported          |
| ------- | ------------------ |
| 2.7.x | :white_check_mark: |
| 2.6.x | :x: |
| 2.5.x | :x: |
| 2.4.x | :x: |
| 2.3.x | :x: |
| 2.2.x | :x: |
| 2.1.x | :x: |
| 2.0.x | :x: |
| 1.10.x | :x: |
| 1.9.x | :x: |
| 1.8.x | :x: |
| 1.7.x | :x: |

## Reporting a Vulnerability

AzzyBot utilizes code scanning via GitHub Actions and various checks from our development team to keep our software as secure as possible for our users, however, no software is perfect.

We recommend using GitHub [Security Advisories](https://github.com/Sella-GH/AzzyBot/security/advisories/new) to report vulnerabilities directly to us.

## In Scope

AzzyBot uses several pieces of upstream software as part of our project's operation. If the security issue being reported is relevant to one of those tools, you should contact the maintainers of that piece of software directly to work toward a resolution, then follow up with our team to inform us when a fix is available that we should incorporate into our software.

You can report any vulnerabilities directly to us if they relate to the core AzzyBot application.

## Out of Scope

AzzyBot is not responsible for the development or maintenance of several pieces of "upstream" software that we incorporate into our own product. This list of software includes, but is not limited to:
* [AzuraCast](https://github.com/AzuraCast/AzuraCast)
* [.NET 9](https://github.com/dotnet/runtime/)
* [CsvHelper](https://github.com/JoshClose/CsvHelper)
* [Docker](https://docker.com/)
* [Docker Compose](https://docker.com/)
* [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
* [EntityFrameworkCore.Exceptions.PostgreSQL](https://github.com/Giorgi/EntityFramework.Exceptions)
* [Lavalink](https://github.com/lavalink-devs/Lavalink)
* [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET)
* [Microsoft.EntityFrameworkCore](https://github.com/dotnet/efcore)
* [Microsoft.Extensions.X Libs](https://github.com/dotnet/runtime)
* [NCronJob](https://github.com/NCronJob-Dev/NCronJob)
* [Npsql](https://github.com/npgsql/npgsql)
* [Npsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg)
* [NReco.Logging.File](https://github.com/NReco/Logging)
* [Roslynator](https://github.com/dotnet/roslynator)
* [SonarAnalyzer.CSharp](https://github.com/SonarSource/sonar-dotnet)
* [System.Linq.Async](https://github.com/dotnet/reactive)
* [TagLibSharp](https://github.com/mono/taglib-sharp)

If you identify a security issue with any of those pieces of software, we encourage you to report it to them directly. If the issue also affects AzzyBot's implementation of the software, please let us know if and when a resolution is available so that we can update our own software to incorporate the fix.
