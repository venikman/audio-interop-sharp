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

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /root/.nuget/packages/microsoft.aspnetcore.app.internal.assets /root/.nuget/packages/microsoft.aspnetcore.app.internal.assets
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AudioSharp.App.dll"]
