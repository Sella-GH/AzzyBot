# BUILD
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
RUN apt install -y wget apt-transport-https gpg libicu72 iputils-ping

# Add backports to solve security issues with CVEs
RUN echo "deb http://deb.debian.org/debian bookworm-backports main" | tee -a /etc/apt/sources.list.d/backports.list
RUN apt update
RUN apt -t bookworm-backports install libgnutls28-dev

# Copy the built app
WORKDIR /app
COPY --from=build /src/out .

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
ENTRYPOINT ["dotnet", "/app/AzzyBot-Docker.dll"]
