# syntax=docker/dockerfile:labs

# BUILD IMAGE
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
USER root
RUN apt update && apt upgrade -y && apt autoremove -y && apt clean -y
WORKDIR /build
COPY ./ ./
ARG CONFIG
COPY ./Nuget.config ./Nuget.config
RUN dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --force --no-cache --ucr \
	&& dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-incremental --no-restore --no-self-contained --ucr \
	&& dotnet publish ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-build --no-restore --no-self-contained -o out --ucr

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim AS runner
USER root

# Add environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en.US.UTF-8
ENV LANG=en.US.UTF-8

# Upgrade internal tools and packages first
RUN apt update && apt upgrade -y && apt autoremove -y && apt clean -y && apt install -y --no-install-recommends iputils-ping libzstd1 && export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib

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

# Dev Build only: Add empty certificate for local testing
RUN touch /etc/ssl/certs/azzybot.crt

# Add new user
RUN useradd -M -U azzy && chown -R azzy:azzy /app && chmod 0755 -R /app
USER azzy
RUN export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib

# Start the app
WORKDIR /app

ENTRYPOINT ["dotnet", "AzzyBot-Docker-Dev.dll"]
