# BUILD
ARG ARCH
FROM mcr.microsoft.com/dotnet/sdk:8.0.203-alpine3.19-$ARCH AS build
WORKDIR /src
COPY ./AzzyBot ./
RUN dotnet restore
RUN dotnet publish -c Docker -o out

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:8.0.3-alpine3.19-$ARCH

# Upgrade internal tools and packages first
RUN echo "@edge https://dl-cdn.alpinelinux.org/alpine/edge/main" >> /etc/apk/repositories
RUN apk add --upgrade apk-tools@edge
RUN apk -U upgrade
RUN apk add --no-cache icu-libs

# Copy the built app
WORKDIR /app
COPY --from=build /src/out .

# Add AdoptOpenJDK 17 Runtime and Lavalink
RUN wget -O /etc/apk/keys/adoptium.rsa.pub https://packages.adoptium.net/artifactory/api/security/keypair/public/repositories/apk
RUN echo 'https://packages.adoptium.net/artifactory/apk/alpine/main' >> /etc/apk/repositories
RUN apk add --no-cache temurin-17-jre
RUN wget -O /app/Modules/MusicStreaming/Files/Lavalink.jar https://github.com/lavalink-devs/Lavalink/releases/download/4.0.4/Lavalink.jar

# Configure Lavalink
ARG GENIUS_TOKEN=test
RUN sed -i "s|Your Genius Client Access Token|${GENIUS_TOKEN}|g" /app/Modules/MusicStreaming/Files/application.yml

# Start the app
WORKDIR /config
USER AzzyBot
ENTRYPOINT ["dotnet", "/app/AzzyBot.dll"]
