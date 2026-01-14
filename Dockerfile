# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*

COPY ["AudioSharp.slnx", "./"]
COPY ["src/AudioSharp.App/AudioSharp.App.csproj", "src/AudioSharp.App/"]
COPY ["tests/AudioSharp.App.Tests/AudioSharp.App.Tests.csproj", "tests/AudioSharp.App.Tests/"]
RUN dotnet restore "AudioSharp.slnx"

COPY . .
WORKDIR /src/src/AudioSharp.App
RUN dotnet publish "AudioSharp.App.csproj" -c Release -o /app/publish --no-restore
RUN if [ ! -f /app/publish/wwwroot/_framework/blazor.web.js ]; then \
      ASSET_PATH="$(find /usr/share/dotnet/packs -path '*/Microsoft.AspNetCore.App.Internal.Assets/*/_framework/blazor.web.js' -print -quit)"; \
      if [ -z "$ASSET_PATH" ]; then \
        ASSET_PATH="$(find /root/.nuget/packages -path '*/microsoft.aspnetcore.app.internal.assets/*/_framework/blazor.web.js' -print -quit)"; \
      fi; \
      if [ -z "$ASSET_PATH" ]; then \
        echo "blazor.web.js not found in SDK packs."; \
        exit 1; \
      fi; \
      mkdir -p /app/publish/wwwroot/_framework; \
      cp "$ASSET_PATH" /app/publish/wwwroot/_framework/blazor.web.js; \
      if [ -f "${ASSET_PATH}.br" ]; then cp "${ASSET_PATH}.br" /app/publish/wwwroot/_framework/blazor.web.js.br; fi; \
      if [ -f "${ASSET_PATH}.gz" ]; then cp "${ASSET_PATH}.gz" /app/publish/wwwroot/_framework/blazor.web.js.gz; fi; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AudioSharp.App.dll"]
