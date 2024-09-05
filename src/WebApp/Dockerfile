# Build image(https://hub.docker.com/_/microsoft-dotnet-core-sdk/)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Cache nuget restore
WORKDIR /src
COPY ["src/WebApp/WebApp.csproj", "src/WebApp/"]
RUN dotnet restore "src/WebApp/WebApp.csproj"

# Copy sources and compile
COPY . .
WORKDIR "/src/src/WebApp"
RUN dotnet build "WebApp.csproj" -c Release -o /app/build

RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish

# Release image
FROM mcr.microsoft.com/dotnet/aspnet:8.0.8-alpine3.20-amd64 AS final

EXPOSE 8080
WORKDIR /app

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache \
    icu-data-full \
    icu-libs

# Copy content from Build image
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebApp.dll"]
