# AzzyBot Development Guide for AI Coding Agents

## Repository Overview

AzzyBot is a Discord music bot written in C# using .NET 10 and DSharpPlus. It's specifically designed to integrate with AzuraCast (internet radio station management software) and provides music streaming capabilities through Lavalink. The bot supports Discord slash commands, file uploads, server management, and various AzuraCast-specific features.

**Key Statistics:**
- **Language**: C# (.NET 10.0)
- **Current Version**: 2.9.0
- **Project Type**: Multi-project Discord bot solution
- **Architecture**: 3-tier (Bot, Core, Data)
- **Database**: PostgreSQL with Entity Framework Core
- **Deployment**: Docker containers with multi-platform support (AMD64/ARM64, Ubuntu/Alpine)
- **CI/CD**: GitHub Actions with extensive quality checks
- **Code Lines**: ~50,000+ lines of C# and MSBuild scripts (varies with updates)

## Build Instructions & Environment Setup

### Prerequisites
1. **.NET 10 SDK** (version 10.0.101 or later) - **CRITICAL**: This project will not build with .NET 9 or earlier
2. **Git**
3. **PostgreSQL** (for local development) or Docker
4. **Lavalink** server (for music streaming features)

### Initial Setup Commands
**Always run these commands in this exact order:**

```bash
# 1. Restore packages with specific config
dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr

# 2. Build the solution
dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c Debug --no-incremental --no-restore --no-self-contained --ucr
```

### Build Configurations
- **Debug**: Development builds (`AzzyBot-Dev.exe` on Windows, `AzzyBot-Dev` on Linux)
- **Release**: Production builds (`AzzyBot.exe` on Windows, `AzzyBot` on Linux)  
- **Docker**: Container production builds (`AzzyBot-Docker.exe` / `AzzyBot-Docker`)
- **Docker-debug**: Container development builds (`AzzyBot-Docker-Dev.exe` / `AzzyBot-Docker-Dev`)

### Build Timing
- **Full clean build**: ~2-3 minutes
- **Incremental build**: ~30-60 seconds
- **Restore packages**: ~1-2 minutes (first time), ~10-30 seconds (subsequent)
- **Docker build**: ~5-10 minutes (depends on platform)

### Common Build Issues & Solutions

**Issue**: `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`
- **Solution**: Install .NET 10 SDK version 10.0.101 or later. **Do not downgrade the target framework.**

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
│   │   ├── Modules/           # Feature modules (Core, MusicStreaming)
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
├── .github/
│   ├── workflows/             # CI/CD pipeline definitions
│   ├── codeql/                # CodeQL configuration
│   └── ISSUE_TEMPLATE/        # Issue templates
├── .vscode/                   # VSCode configuration
├── legal/                     # Legal documents and licenses
├── Directory.Build.props      # MSBuild global properties
├── Directory.Packages.props   # Centralized package versions
├── global.json               # .NET SDK version lock (10.0.101)
├── Nuget.config              # Package source configuration
├── dev.Dockerfile             # Development Docker image
├── dev.alpine.Dockerfile      # Development Alpine Docker image
├── release.Dockerfile         # Production Docker image
├── release.alpine.Dockerfile  # Production Alpine Docker image
├── docker-compose.yml         # Production Docker Compose
├── docker-compose.dev.yml     # Development Docker Compose
└── AzzyBot.slnx              # Solution file
```

### Key Configuration Files
- **Directory.Build.props**: Global MSBuild settings, analyzer configuration, build optimizations
- **Directory.Packages.props**: Centralized package version management (enables `ManagePackageVersionsCentrally`)
- **global.json**: Locks .NET SDK to version 10.0.101 with `allowPrerelease: true` and `rollForward: latestFeature`
- **.editorconfig**: Extensive code style and analyzer rules (1106 lines)
- **Nuget.config**: Package sources and restore settings

### Module Organization
The bot uses a modular architecture with two main modules:
- **Core Module**: Administrative, utility commands, and AzuraCast integration features
- **MusicStreaming Module**: Lavalink-based music playback for SoundCloud and other providers

## GitHub Actions & CI/CD Pipeline

### Workflow Files
- **docker.yml**: Multi-platform Docker image builds (Ubuntu/Alpine, AMD64/ARM64)
- **sonarcloud.yml**: Code quality analysis with SonarCloud
- **codeql-csharp.yml**: Security analysis with GitHub CodeQL for C# code
- **codeql-actions.yml**: Security analysis with GitHub CodeQL for GitHub Actions
- **hcl-appscan-codesweep.yml**: Additional security scanning with HCL AppScan CodeSweep
- **dependabot.yml**: Automated dependency updates configuration
- **dependabot-auto-merge.yml**: Automatic merging of Dependabot PRs
- **dependabot-formatting.yml**: Auto-formatting for Dependabot PRs
- **release.yml**: Automated releases with tagging and artifact building
- **tag-creation.yml**: Automated tag creation workflow (reusable)
- **backports.yml**: Automated backporting of changes to release branches

### CI/CD Process
1. **Code Push** triggers workflows on paths: `**.cs`, `**.csproj`, `**.json`, `**.props`, `**.sln`, `**.yml`
2. **Build Process**:
        ```bash
        # Checkout
        # Build (Windows for SonarCloud, Ubuntu for CodeQL)
        dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr
        dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c Release --no-incremental --no-restore --no-self-contained --ucr
        ```
4. **Quality Checks**: 
   - SonarCloud analysis (code quality, bugs, code smells)
   - CodeQL security scanning for C# code and GitHub Actions
   - HCL AppScan CodeSweep (additional security scanning on PRs)
5. **Docker Builds**: Multi-platform images (linux/amd64, linux/arm64) for both Ubuntu and Alpine, pushed to Docker Hub
6. **Release Process**: 
   - Automated on commit messages containing `[release]` or `[pre-release]`
   - Creates git tags automatically
   - Builds Docker images and .NET binaries
   - Generates release notes from CHANGELOG.md
   - Publishes to GitHub Releases and Docker Hub
7. **Dependabot Integration**:
   - Daily dependency checks for NuGet packages, GitHub Actions, and .NET SDK
   - Auto-formatting of dependabot PRs
   - Auto-merge capability for minor/patch updates
8. **Backports**: Automated backporting to release branches when needed

### Build Matrix
- **Platforms**: linux/amd64, linux/arm64
- **OS Images**: Ubuntu 24.04 (default), Alpine (lightweight)
- **Runners**: 
  - `ubuntu-24.04` for standard builds
  - `ubuntu-slim` for metadata extraction
  - `windows-2025` for SonarCloud analysis (requires Windows for .NET Framework analyzers)

## Development Environment

### Recommended Setup
1. **IDE**: Visual Studio 2026 Community (as mentioned in README)
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

# Stop the environment
docker-compose -f docker-compose.dev.yml down
```

**Available Docker Images:**
- `sellagh/azzybot:latest` - Production Ubuntu-based image
- `sellagh/azzybot:dev` - Development Ubuntu-based image
- `sellagh/azzybot:latest-alpine` - Production Alpine-based image (smaller size)
- `sellagh/azzybot:dev-alpine` - Development Alpine-based image

**Multi-Platform Support:**
- linux/amd64 (x86-64)
- linux/arm64 (ARM64/AArch64)

## Code Quality & Standards

### Analysis Tools
The project uses extensive static analysis:
- **Roslynator**: 200+ C# code analysis rules
- **SonarAnalyzer.CSharp**: Code quality and security analysis  
- **EditorConfig**: Strict formatting and style rules
- **CodeQL**: Security vulnerability scanning

### Coding Standards
- **C# Version**: Modern C# with .NET 10 features
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Code Style**: File-scoped namespaces, expression-bodied members, pattern matching
- **Architecture**: Clean separation between Bot, Core, and Data layers

### Build Properties
- **WarningsAsErrors**: `nullable` (null reference warnings become errors)
- **AnalysisLevel**: `latest-all` (maximum analysis coverage)
- **EnforceCodeStyleInBuild**: Enabled for strict code style enforcement
- **Features**: `Strict` (enables strict compiler mode for more safety)
- **CheckForOverflowUnderflow**: Enabled for arithmetic safety
- **UseArtifactsOutput**: Enabled (uses new artifacts output structure)
- **NuGetAuditMode**: `all` (enables NuGet package vulnerability auditing)

### Runtime Configuration
- **HTTP/3 Support**: Explicitly disabled (`System.Net.SocketsHttpHandler.Http3Support = false`) until .NET fixes it
- **JIT Settings**: TieredCompilation, QuickJit, and TieredPGO enabled for optimal performance
- **Garbage Collection**: Server GC with concurrent collection and adaptive mode
- **Globalization**: InvariantGlobalization disabled (full globalization support)
- **JSON Serialization**: Source generators enabled (`JsonSerializerIsReflectionEnabledByDefault = false`)
- **Configuration Binding**: Source generators enabled (`EnableConfigurationBindingGenerator = true`)

### File Formatting Standards
When creating or modifying files in the repository, ensure files are saved with the following settings:
- **Indentation**: 4 spaces (no tabs) for files in the `src` folder only
- **Line Endings**: CRLF (Windows-style line endings)

These standards ensure consistency across the codebase and prevent formatting conflicts in commits.

## Database & Entity Framework

### Database Setup
- **Provider**: PostgreSQL via Npgsql.EntityFrameworkCore.PostgreSQL
- **Migrations**: Located in `src/AzzyBot.Data/Migrations/`
- **Configuration**: Connection strings in `Settings/AzzyBotSettings.json`
- **SSL Support**: Available as of version 2.5.0 (optional configuration)
- **Version Optimization**: Optional database version specification for improved SQL translation

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
2. Reference in appropriate `.csproj` file (without version - version is managed centrally)
3. If the package should not be auto-updated by Dependabot, add it to the ignore list in `.github/dependabot.yml`
4. Consider which Dependabot group it belongs to (Analyzers, Microsoft, or Others)

### Modifying Build Configuration
1. **Global changes**: Edit `Directory.Build.props`
2. **Project-specific**: Modify individual `.csproj` files
3. **Dependencies**: Update `Directory.Packages.props`
4. **Analyzer rules**: Modify `.editorconfig`

## Troubleshooting Guide

### "Project failed to restore"
1. Ensure .NET 10 SDK is installed: `dotnet --list-sdks` (version 10.0.101 or later)
2. Clear NuGet cache: `dotnet nuget locals all --clear`
3. Use correct restore command with `--configfile ./Nuget.config`

### "Build is taking too long"
The project has extensive code analysis that can slow builds:
- First build: Allow 5+ minutes
- Use `dotnet build --no-incremental` for clean builds
- Consider disabling analyzers temporarily for faster iteration

### "Docker build failures"
1. Ensure multi-platform buildx is enabled
2. Check Dockerfile paths match the build context (use correct dev/release Dockerfile)
3. Verify the correct Alpine vs Ubuntu Dockerfile is being used
4. Check that build configuration matches the Dockerfile (Docker vs Docker-debug)

## Security & Encryption

### Data Protection
- **Encryption Algorithm**: AES-GCM with 256-bit keys (as of version 2.7.0)
  - Migrated from AES-CCM for future-proofing (.NET 10 deprecates AesCCM on some platforms)
  - Automatic migration for existing data during startup (limited migration period)
  - All sensitive data in the database is encrypted at rest

### Security Scanning
The project uses multiple security scanning tools:
1. **CodeQL**: Automated security vulnerability scanning for C# code and GitHub Actions
2. **HCL AppScan CodeSweep**: Additional security scanning on pull requests
3. **NuGet Audit**: Enabled via `NuGetAuditMode: all` to detect vulnerable dependencies
4. **SonarCloud**: Detects security hotspots and vulnerabilities in code

### Security Best Practices
- Never commit secrets or credentials to the repository
- All API keys and tokens must be stored in `AzzyBotSettings.json` (gitignored)
- Database connection strings should use environment variables in production
- HTTPS is enforced for all Discord URI protocols

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
- `src/AzzyBot.Bot/Settings/AzzyBotSettings.json` - Application configuration (not in repo, created from template)
- `src/AzzyBot.Bot/Settings/AzzyBotSettings-Dev.json` - Development template
- `src/AzzyBot.Bot/Settings/AzzyBotSettings-Docker.json` - Docker template
- `src/AzzyBot.Bot/Modules/MusicStreaming/Files/application.yml` - Lavalink config
- `src/AzzyBot.Bot/Modules/Core/Files/AppStats.json` - Application statistics

**Resources:**
- `src/AzzyBot.Bot/Resources/UriStrings.resx` - URI string resources with code generation

**Trust these instructions** - they are verified against the actual codebase (version 2.9.0) and CI/CD processes. Only search for additional information if these instructions are incomplete or appear incorrect based on build failures.

## Additional Notes

### DebugCommands.cs
- Located in `src/AzzyBot.Bot/Commands/DebugCommands.cs`
- **Only compiled in Debug and Docker-debug configurations**
- Excluded from Release and Docker builds (production)
- Contains development and testing commands not meant for production

### Platforms Configuration
- Configured for: `x64`, `ARM64`, `AnyCPU`
- Platform target: `AnyCPU` (allows running on any architecture)
- Architecture mismatch warnings suppressed (`ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch: None`)

### Documentation Generation
- XML documentation files generated for all projects (`GenerateDocumentationFile: True`)
- Helps with IntelliSense and API documentation
- Required workaround for Roslyn issue #41640

### Implicit Usings
- **Disabled** (`ImplicitUsings: disable`)
- All using statements must be explicit
- Improves code clarity and reduces ambiguity

### Build Output
- Uses new artifacts output structure (`UseArtifactsOutput: true`)
- Centralizes build outputs in `artifacts/` directory
- Improves build performance and organization
