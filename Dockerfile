# BUILD IMAGE
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
WORKDIR /src
COPY ./AzzyBot ./
RUN dotnet restore ./AzzyBot-Next.csproj
ARG CONFIG
RUN dotnet publish ./AzzyBot-Next.csproj -c $CONFIG -o out

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim

# Upgrade internal tools and packages first
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
RUN apt install -y --no-install-recommends iputils-ping

# Copy the built app
WORKDIR /app
COPY --from=build /src/out .

# Add commit, timestamp and lines of code
ARG COMMIT
ARG TIMESTAMP
ARG LOC_CS
ARG LOC_JSON
RUN sed -i "s\Commit not found\\$COMMIT\g" /app/Modules/Core/Files/AzzyBotStats.json
RUN sed -i "s\Compile date not found\\$TIMESTAMP\g" /app/Modules/Core/Files/AzzyBotStats.json
RUN sed -i "s\Lines of source code not found\\$LOC_CS\g" /app/Modules/Core/Files/AzzyBotStats.json
RUN sed -i "s\Lines of JSON code not found\\$LOC_JSON\g" /app/Modules/Core/Files/AzzyBotStats.json

# Add new user
RUN groupadd azzy
RUN useradd -m -s /bin/bash -g azzy azzy
RUN chown -R azzy:azzy /app
RUN chmod 0755 -R /app
USER azzy

# Start the app
WORKDIR /config
ARG CONFIG
ARG DLL=AzzyBot-Next-Docker.dll
RUN if [ "$CONFIG" = "Docker-debug" ]; then DLL="AzzyBot-Next-Docker-Dev.dll"; fi

ENTRYPOINT ["dotnet", $DLL]
