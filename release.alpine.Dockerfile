# syntax=docker/dockerfile:labs

# BUILD IMAGE
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
USER root

RUN --mount=type=cache,target=/var/cache/apk,sharing=locked,id=apk-build \
    apk update \
  && apk upgrade

WORKDIR /build

# Restore layer: only invalidated by dependency/project-graph file changes
ARG CONFIG
COPY ./Nuget.config ./Nuget.config
COPY ./global.json ./global.json
COPY ./Directory.Build.props ./Directory.Build.props
COPY ./Directory.Packages.props ./Directory.Packages.props
COPY ./AzzyBot.slnx ./AzzyBot.slnx
COPY ./src/AzzyBot.Bot/AzzyBot.Bot.csproj ./src/AzzyBot.Bot/AzzyBot.Bot.csproj
COPY ./src/AzzyBot.Core/AzzyBot.Core.csproj ./src/AzzyBot.Core/AzzyBot.Core.csproj
COPY ./src/AzzyBot.Data/AzzyBot.Data.csproj ./src/AzzyBot.Data/AzzyBot.Data.csproj

RUN dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --ucr

# Build/publish layer: invalidated by any source change, but restore above stays cached
COPY ./ ./

RUN dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-incremental --no-restore --no-self-contained --ucr \
  && dotnet publish ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-build --no-restore --no-self-contained -o out --ucr

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runner
USER root

# Upgrade internal tools and packages first
RUN --mount=type=cache,target=/var/cache/apk,sharing=locked,id=apk-runner \
    apk update \
  && apk upgrade \
  && apk add --no-cache icu-data-full icu-libs iputils-ping krb5-libs libmsquic tzdata zstd-dev

# Add environment variables
ENV PATH="/usr/local/zstd:${PATH}" \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en.US.UTF-8 \
    LANG=en.US.UTF-8

# Copy the built app
WORKDIR /app
COPY --exclude=*.xml --from=build --chown=app:app --chmod=0755 /build/out .

# Use built-in dotnet app user
USER app

# Start the app
WORKDIR /app

ENTRYPOINT ["./AzzyBot-Docker"]
