# syntax=docker/dockerfile:labs

# BUILD IMAGE
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
USER root

RUN apt update && apt upgrade -y && apt autoremove -y && apt clean -y

WORKDIR /build
COPY ./ ./

ARG CONFIG
COPY ./Nuget.config ./Nuget.config

# Restore, build, and publish the bot
RUN dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr \
	&& dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-incremental --no-restore --no-self-contained --ucr \
	&& dotnet publish ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-build --no-restore --no-self-contained -o out --ucr

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:9.0-noble AS runner
USER root

# Upgrade internal tools and packages first
RUN apt update && apt upgrade -y && apt install -y --no-install-recommends iputils-ping libzstd-dev && apt autoremove --purge -y && apt clean -y && rm -rf /var/lib/apt/lists/*

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

ENTRYPOINT ["/bin/bash", "-c", "AzzyBot-Docker"]
