# BUILD
ARG ARCH
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-$ARCH AS build
WORKDIR /src
COPY ./AzzyBot ./
RUN dotnet restore --force --no-cache
RUN dotnet publish -c Docker -o out

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-$ARCH

# Upgrade internal tools and packages first
USER root
RUN apt update && apt upgrade -y
RUN apt install -y wget apt-transport-https gpg libicu72

# Copy the built app
WORKDIR /app
COPY --from=build /src/out .

# Add AdoptOpenJDK 17 Runtime and Lavalink
RUN wget -qO - https://packages.adoptium.net/artifactory/api/gpg/key/public | gpg --dearmor | tee /etc/apt/trusted.gpg.d/adoptium.gpg > /dev/null
RUN echo "deb https://packages.adoptium.net/artifactory/deb $(awk -F= '/^VERSION_CODENAME/{print$2}' /etc/os-release) main" | tee /etc/apt/sources.list.d/adoptium.list
RUN apt update && apt upgrade -y
RUN apt install -y temurin-17-jre
RUN wget -qO /app/Modules/MusicStreaming/Files/Lavalink.jar https://github.com/lavalink-devs/Lavalink/releases/download/4.0.4/Lavalink.jar

# Configure Lavalink
ARG GENIUS_TOKEN=test
RUN sed -i "s|Your Genius Client Access Token|${GENIUS_TOKEN}|g" /app/Modules/MusicStreaming/Files/application.yml

# Add new user
RUN groupadd azzy
RUN useradd -m -s /bin/bash -g azzy azzy
RUN chown -R /app azzy:azzy
RUN chmod 0755 -R /app
USER azzy

# Start the app
WORKDIR /config
ENTRYPOINT ["dotnet", "/app/AzzyBot.dll"]
