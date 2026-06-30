# syntax=docker/dockerfile:labs

# --- BUILD IMAGE ---
FROM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build
USER root

RUN --mount=type=cache,target=/var/cache/apt,sharing=locked,id=apt-resolute \
    --mount=type=cache,target=/var/lib/apt,sharing=locked,id=apt-lib-resolute \
    apt-get update \
  && apt-get upgrade -y \
  && apt-get autoremove -y

WORKDIR /build

# Restore layer: only invalidated by dependency/project-graph file changes
ARG CONFIG
COPY ./Nuget.config ./Nuget.config
COPY ./global.json ./global.json
COPY ./Directory.Build.props ./Directory.Build.props
COPY ./Directory.Packages.props ./Directory.Packages.props
COPY ./AzzyBot.slnx ./AzzyBot.slnx
COPY ./src/AzzyBot.Bot/AzzyBot.Bot.csproj ./src/AzzyBot.Bot/AzzyBot.Bot.csproj
COPY ./src/AzzyBot.Core/AzzyBot.Core.csproj ./src/AzzyBot.Core/AzzyBot.Core.csproj
COPY ./src/AzzyBot.Data/AzzyBot.Data.csproj ./src/AzzyBot.Data/AzzyBot.Data.csproj

RUN dotnet restore ./src/AzzyBot.Bot/AzzyBot.Bot.csproj --configfile ./Nuget.config --ucr

# Build/publish layer: invalidated by any source change, but restore above stays cached
COPY ./ ./

RUN dotnet build ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-incremental --no-restore --no-self-contained --ucr \
  && dotnet publish ./src/AzzyBot.Bot/AzzyBot.Bot.csproj -c $CONFIG --no-build --no-restore --no-self-contained -o out --ucr

ADD https://packages.microsoft.com/config/ubuntu/26.04/packages-microsoft-prod.deb /packages-microsoft-prod.deb

# --- RUNNER IMAGE ---
FROM mcr.microsoft.com/dotnet/runtime:10.0-resolute AS runner
USER root

# Upgrade internal tools and packages first
RUN --mount=type=bind,from=build,source=/packages-microsoft-prod.deb,target=/tmp/packages-microsoft-prod.deb \
    --mount=type=cache,target=/var/cache/apt,sharing=locked,id=apt-resolute-runner \
    --mount=type=cache,target=/var/lib/apt,sharing=locked,id=apt-lib-resolute-runner \
  apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates gnupg \
  && dpkg -i /tmp/packages-microsoft-prod.deb \
  && apt-get update \
  && apt-get upgrade -y \
  && apt-get install -y --no-install-recommends iputils-ping libgssapi-krb5-2 libzstd-dev \
  && apt-get install -y --no-install-recommends libmsquic libxdp1 libnl-3-200 libnl-route-3-200 \
  && apt-get autoremove --purge -y

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

ENTRYPOINT ["./AzzyBot-Docker"]
