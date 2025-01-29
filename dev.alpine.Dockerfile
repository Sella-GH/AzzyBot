# syntax=docker/dockerfile:labs

# BUILD IMAGE
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
USER root
RUN apk update && apk upgrade && apk cache sync
WORKDIR /build
COPY ./ ./
ARG CONFIG
COPY ./Nuget.config ./Nuget.config
RUN dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr \
	&& dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-incremental --no-restore --no-self-contained --ucr \
	&& dotnet publish ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-build --no-restore --no-self-contained -o out --ucr

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS runner
USER root

# Upgrade internal tools and packages first
RUN apk update && apk upgrade && apk add --no-cache icu-data-full icu-libs iputils-ping sed tzdata zstd-dev && apk cache sync && rm -rf /var/cache/apk/*

# Add environment variables
ENV PATH="/usr/local/zstd:${PATH}"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en.US.UTF-8
ENV LANG=en.US.UTF-8

# Copy the built app
WORKDIR /app
COPY --exclude=*.xml --from=build /build/out .

# Add commit, timestamp and lines of code
ARG COMMIT
ARG TIMESTAMP
ARG LOC_CS
RUN sed -i "s\Commit not found\\$COMMIT\g" /app/Modules/Core/Files/AppStats.json \
	&& sed -i "s\Compilation date not found\\$TIMESTAMP\g" /app/Modules/Core/Files/AppStats.json \
	&& sed -i "s\Lines of source code not found\\$LOC_CS\g" /app/Modules/Core/Files/AppStats.json

# Add new user
RUN adduser -D -H azzy && chown -R azzy:azzy /app && chmod 0755 -R /app
USER azzy

# Start the app
WORKDIR /app

ENTRYPOINT ["dotnet", "AzzyBot-Docker-Dev.dll"]
