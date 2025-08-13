# AzzyBot Development Guide for AI Coding Agents

## Repository Overview

AzzyBot is a Discord music bot written in C# using .NET 9 and DSharpPlus. It's specifically designed to integrate with AzuraCast (internet radio station management software) and provides music streaming capabilities through Lavalink. The bot supports Discord slash commands, file uploads, server management, and various AzuraCast-specific features.

**Key Statistics:**
- **Language**: C# (.NET 9.0)
- **Project Type**: Multi-project Discord bot solution
- **Architecture**: 3-tier (Bot, Core, Data) + External submodules
- **Database**: PostgreSQL with Entity Framework Core
- **Deployment**: Docker containers with multi-platform support
- **CI/CD**: GitHub Actions with extensive quality checks

## Build Instructions & Environment Setup

### Prerequisites
1. **.NET 9 SDK** (version 9.0.304) - **CRITICAL**: This project will not build with .NET 8 or earlier
2. **Git** with submodule support
3. **PostgreSQL** (for local development) or Docker
4. **Lavalink** server (for music streaming features)

### Initial Setup Commands
**Always run these commands in this exact order:**

```bash
# 1. Initialize submodules (MANDATORY - project will fail without this)
git submodule update --init --recursive

# 2. Restore packages with specific config
dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr

# 3. Build the solution
dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c Debug --no-incremental --no-restore --no-self-contained --ucr
```

### Build Configurations
- **Debug**: Development builds (`AzzyBot-Dev.exe`)
- **Release**: Production builds (`AzzyBot.exe`)  
- **Docker**: Container production builds (`AzzyBot-Docker.exe`)
- **Docker-debug**: Container development builds (`AzzyBot-Docker-Dev.exe`)

### Build Timing
- **Full clean build**: ~2-3 minutes
- **Incremental build**: ~30-60 seconds
- **Restore packages**: ~1-2 minutes (first time), ~10-30 seconds (subsequent)
- **Docker build**: ~5-10 minutes (depends on platform)

### Common Build Issues & Solutions

**Issue**: `NETSDK1045: The current .NET SDK does not support targeting .NET 9.0`
- **Solution**: Install .NET 9 SDK version 9.0.304 or later. **Do not downgrade the target framework.**

**Issue**: `Submodule 'extern/Lavalink4NET' not found`
- **Solution**: Run `git submodule update --init --recursive` before any build commands

**Issue**: Package restore failures
- **Solution**: Use the `--configfile ./Nuget.config` parameter and ensure you have internet connectivity

**Issue**: Build hanging during analysis
- **Solution**: The project has extensive code analysis (Roslynator, SonarAnalyzer). On slower machines, allow 5+ minutes for first build.

## Project Architecture & Layout

### Solution Structure
```
AzzyBot/
├── src/
│   ├── AzzyBot.Bot/           # Main executable project
│   │   ├── Commands/          # Discord slash command implementations
│   │   ├── Extensions/        # C# extension methods
│   │   ├── Modules/           # Feature modules (AzuraCast, MusicStreaming, etc.)
│   │   ├── Services/          # Business logic services
│   │   ├── Settings/          # Configuration files and classes
│   │   ├── Utilities/         # Helper classes and enums
│   │   └── Startup.cs         # Application entry point
│   ├── AzzyBot.Core/          # Shared core library
│   │   ├── Extensions/        # Core extension methods
│   │   ├── Logging/           # Logging infrastructure
│   │   └── Utilities/         # Core utilities and helpers
│   └── AzzyBot.Data/          # Data access layer
│       ├── Entities/          # EF Core entity models
│       ├── Migrations/        # Database migrations
│       ├── Services/          # Data access services
│       └── Settings/          # Database configuration
├── extern/
│   └── Lavalink4NET/          # Git submodule (custom fork)
├── .github/
│   └── workflows/             # CI/CD pipeline definitions
├── Directory.Build.props      # MSBuild global properties
├── Directory.Packages.props   # Centralized package versions
├── global.json               # .NET SDK version lock
├── Nuget.config              # Package source configuration
└── AzzyBot.slnx              # Solution file
```

### Key Configuration Files
- **Directory.Build.props**: Global MSBuild settings, analyzer configuration, build optimizations
- **Directory.Packages.props**: Centralized package version management (enables `ManagePackageVersionsCentrally`)
- **global.json**: Locks .NET SDK to version 9.0.304
- **.editorconfig**: Extensive code style and analyzer rules (1000+ lines)
- **Nuget.config**: Package sources and restore settings

### Module Organization
The bot uses a modular architecture:
- **AzuraCast Module**: Integration with AzuraCast radio software
- **MusicStreaming Module**: Lavalink-based music playback
- **Core Module**: Administrative and utility commands

## GitHub Actions & CI/CD Pipeline

### Workflow Files
- **docker.yml**: Multi-platform Docker image builds (Ubuntu/Alpine, AMD64/ARM64)
- **sonarcloud.yml**: Code quality analysis with SonarQube
- **codeql-csharp.yml**: Security analysis with GitHub CodeQL
- **dependabot.yml**: Automated dependency updates
- **release.yml**: Automated releases with tagging

### CI/CD Process
1. **Code Push** triggers workflows on paths: `**.cs`, `**.csproj`, `**.json`, `**.props`, `**.sln`
2. **Build Process**:
   ```bash
   # Checkout with submodules
   git submodule update --init --recursive
   
   # Build (Windows for SonarCloud, Ubuntu for CodeQL)
   dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr
   dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c Release --no-incremental --no-restore --no-self-contained --ucr
   ```
3. **Quality Checks**: SonarCloud analysis, CodeQL security scanning
4. **Docker Builds**: Multi-platform images pushed to Docker Hub
5. **Release Process**: Automated on version tags

### Build Matrix
- **Platforms**: linux/amd64, linux/arm64
- **OS Images**: Ubuntu (default), Alpine (lightweight)
- **Runners**: Standard GitHub runners + ARM64 runners for native builds

## Development Environment

### Recommended Setup
1. **IDE**: Visual Studio 2022 Community (as mentioned in README)
2. **Extensions**: C# Dev Kit, Docker, GitHub Actions
3. **Local Services**: PostgreSQL, Lavalink (via Docker Compose)

### VSCode Configuration
The repository includes `.vscode/` configuration:
- **launch.json**: Debug configuration for `AzzyBot-Dev.dll`
- **tasks.json**: Build task for the main project
- **settings.json**: Editor settings

### Docker Development
```bash
# Start development environment
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose logs -f AzzyBot
```

## Code Quality & Standards

### Analysis Tools
The project uses extensive static analysis:
- **Roslynator**: 200+ C# code analysis rules
- **SonarAnalyzer.CSharp**: Code quality and security analysis  
- **EditorConfig**: Strict formatting and style rules
- **CodeQL**: Security vulnerability scanning

### Coding Standards
- **C# Version**: Modern C# with .NET 9 features
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Code Style**: File-scoped namespaces, expression-bodied members, pattern matching
- **Architecture**: Clean separation between Bot, Core, and Data layers

### Build Properties
- **WarningsAsErrors**: `nullable` (null reference warnings become errors)
- **AnalysisLevel**: `latest-all` (maximum analysis coverage)
- **TreatWarningsAsErrors**: Enabled for code quality enforcement

### File Formatting Standards
When creating or modifying files in the repository, ensure all files are saved with the following settings:
- **Indentation**: 4 spaces (no tabs)
- **Line Endings**: CRLF (Windows-style line endings)

These standards ensure consistency across the codebase and prevent formatting conflicts in commits.

## Database & Entity Framework

### Database Setup
- **Provider**: PostgreSQL via Npgsql.EntityFrameworkCore.PostgreSQL
- **Migrations**: Located in `src/AzzyBot.Data/Migrations/`
- **Configuration**: Connection strings in `Settings/AzzyBotSettings.json`

### EF Core Commands
```bash
# Add migration
dotnet ef migrations add MigrationName --project src/AzzyBot.Data --startup-project src/AzzyBot.Bot

# Update database
dotnet ef database update --project src/AzzyBot.Data --startup-project src/AzzyBot.Bot
```

## Common Development Tasks

### Adding New Discord Commands
1. Create command class in `src/AzzyBot.Bot/Commands/`
2. Use DSharpPlus.Commands attributes (`[Command]`, `[Description]`)
3. Implement permission checks via custom attributes
4. Add resource strings in `Resources/` if needed

### Adding New Dependencies
1. Add to `Directory.Packages.props` (centralized version management)
2. Reference in appropriate `.csproj` file
3. Update dependabot.yml ignore lists if needed

### Modifying Build Configuration
1. **Global changes**: Edit `Directory.Build.props`
2. **Project-specific**: Modify individual `.csproj` files
3. **Dependencies**: Update `Directory.Packages.props`
4. **Analyzer rules**: Modify `.editorconfig`

## Troubleshooting Guide

### "Project failed to restore"
1. Ensure .NET 9 SDK is installed: `dotnet --list-sdks`
2. Check submodules: `git submodule status`
3. Clear NuGet cache: `dotnet nuget locals all --clear`
4. Use correct restore command with `--configfile ./Nuget.config`

### "Build is taking too long"
The project has extensive code analysis that can slow builds:
- First build: Allow 5+ minutes
- Use `dotnet build --no-incremental` for clean builds
- Consider disabling analyzers temporarily for faster iteration

### "Docker build failures"
1. Ensure multi-platform buildx is enabled
2. Check Dockerfile paths match the build context
3. Verify submodules are properly initialized in the container

## Key Files for Agents

**Configuration Changes:**
- `Directory.Build.props` - Global build settings
- `Directory.Packages.props` - Package versions
- `.editorconfig` - Code style rules

**Core Logic:**
- `src/AzzyBot.Bot/Startup.cs` - Application bootstrap
- `src/AzzyBot.Bot/Commands/` - Discord command implementations
- `src/AzzyBot.Data/Services/` - Database operations

**Settings:**
- `src/AzzyBot.Bot/Settings/AzzyBotSettings.json` - Application configuration
- `src/AzzyBot.Bot/Modules/MusicStreaming/Files/application.yml` - Lavalink config

**Trust these instructions** - they are verified against the actual codebase and CI/CD processes. Only search for additional information if these instructions are incomplete or appear incorrect based on build failures.