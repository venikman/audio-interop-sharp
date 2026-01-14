# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY ["src/AudioSharp.App/AudioSharp.App.csproj", "src/AudioSharp.App/"]
RUN dotnet restore "src/AudioSharp.App/AudioSharp.App.csproj"

COPY . .
WORKDIR /src/src/AudioSharp.App
RUN dotnet publish "AudioSharp.App.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AudioSharp.App.dll"]
