# BUILD IMAGE
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
WORKDIR /build
COPY ./src/AzzyBot-Next ./
RUN dotnet restore ./AzzyBot-Next.csproj
ARG ARCH
ARG CONFIG
ARG OS
RUN dotnet publish ./AzzyBot-Next.csproj -a $ARCH -c $CONFIG --os $OS -o out

# RUNNER IMAGE
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim

# Upgrade internal tools and packages first
USER root
RUN apt update && apt upgrade -y && apt autoremove -y
RUN apt install -y --no-install-recommends iputils-ping postgresql-client

# Copy the built app
WORKDIR /app
COPY --from=build /build/out .

# Add commit, timestamp and lines of code
ARG COMMIT
ARG TIMESTAMP
ARG LOC_CS
RUN sed -i "s\Commit not found\\$COMMIT\g" /app/Modules/Core/Files/AzzyBotStats.json
RUN sed -i "s\Compilation date not found\\$TIMESTAMP\g" /app/Modules/Core/Files/AzzyBotStats.json
RUN sed -i "s\Lines of source code not found\\$LOC_CS\g" /app/Modules/Core/Files/AzzyBotStats.json

# Add new user
RUN groupadd azzy
RUN useradd -m -s /bin/bash -g azzy azzy
RUN chown -R azzy:azzy /app
RUN chmod 0755 -R /app
USER azzy

# Start the app
WORKDIR /app

ENTRYPOINT ["dotnet", "AzzyBot-Docker-Dev.dll"]
