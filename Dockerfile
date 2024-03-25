# BUILD
ARG ARCH
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-$ARCH AS build
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
WORKDIR /src
COPY ./AzzyBot ./
RUN dotnet restore ./AzzyBot.csproj
RUN dotnet publish -c Docker -o out

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-$ARCH

# Upgrade internal tools and packages first
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
RUN apt install -y wget apt-transport-https gpg libicu72 iputils-ping

# Copy the built app
WORKDIR /app
COPY --from=build /src/out .

# Add AdoptOpenJDK 17 Runtime and Lavalink
RUN wget -qO - https://packages.adoptium.net/artifactory/api/gpg/key/public | gpg --dearmor | tee /etc/apt/trusted.gpg.d/adoptium.gpg > /dev/null
RUN echo "deb https://packages.adoptium.net/artifactory/deb $(awk -F= '/^VERSION_CODENAME/{print$2}' /etc/os-release) main" | tee /etc/apt/sources.list.d/adoptium.list
RUN apt update && apt upgrade -y && apt autoremove -y
RUN apt install -y temurin-17-jre
RUN wget -qO /app/Modules/MusicStreaming/Files/Lavalink.jar https://github.com/lavalink-devs/Lavalink/releases/download/4.0.4/Lavalink.jar
RUN mkdir -p /app/Modules/MusicStreaming/Files/plugins && wget -qO /app/Modules/MusicStreaming/Files/plugins/java-lyrics-plugin-1.6.2.jar https://maven.lavalink.dev/releases/me/duncte123/java-lyrics-plugin/1.6.2/java-lyrics-plugin-1.6.2.jar

# Configure Lavalink
ENV GENIUS_COUNTRY_CODE=de
ENV GENIUS_API_KEY=empty
ENV LAVALINK_PASSWORD=youshallnotpass
RUN sed -i "s|countryCode: de|countryCode: ${GENIUS_COUNTRY_CODE}|g" /app/Modules/MusicStreaming/Files/application.yml
RUN sed -i "s|Your Genius Client Access Token|${GENIUS_API_KEY}|g" /app/Modules/MusicStreaming/Files/application.yml
RUN sed -i "s|youshallnotpass|${LAVALINK_PASSWORD}|g" /app/Modules/MusicStreaming/Files/application.yml

# Add commit and timestamp
ARG COMMIT
ARG TIMESTAMP
RUN echo $COMMIT > /app/Commit.txt
RUN echo $TIMESTAMP > /app/BuildDate.txt

# Add new user
RUN groupadd azzy
RUN useradd -m -s /bin/bash -g azzy azzy
RUN chown -R azzy:azzy /app
RUN chmod 0755 -R /app
USER azzy

# Start the app
WORKDIR /config
ENTRYPOINT ["dotnet", "/app/AzzyBot.dll"]
