# BUILD IMAGE
ARG ARCH
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-$ARCH AS build
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
WORKDIR /src
COPY ./AzzyBot ./
RUN dotnet restore ./AzzyBot.csproj
RUN dotnet publish ./AzzyBot.csproj -c Docker -o out

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-$ARCH

# Upgrade internal tools and packages first
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
RUN apt install -y --no-install-recommends wget apt-transport-https gpg libicu72 iputils-ping

# Copy the built app
WORKDIR /app
COPY --from=build /src/out .

# Add commit, timestamp and lines of code
ARG COMMIT
ARG TIMESTAMP
ARG LOC_CS
ARG LOC_JSON
RUN sed -i "s\Commit not found\$COMMIT\g" /app/AzzyBot.json
RUN sed -i "s\Compile date not found\$TIMESTAMP\g" /app/AzzyBot.json
RUN sed -i "s\Lines of source code not found\$LOC_CS\g" /app/AzzyBot.json
RUN sed -i "s\Lines of JSON code not found\$LOC_JSON\g" /app/AzzyBot.json

# Add new user
RUN groupadd azzy
RUN useradd -m -s /bin/bash -g azzy azzy
RUN chown -R azzy:azzy /app
RUN chmod 0755 -R /app
USER azzy

# Start the app
WORKDIR /config
ENTRYPOINT ["dotnet", "/app/AzzyBot-Docker.dll"]
