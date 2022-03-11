# Build image(https://hub.docker.com/_/microsoft-dotnet-core-sdk/)
FROM mcr.microsoft.com/dotnet/sdk:6.0.201-alpine3.15-amd64 AS build

# Install to build containers
# - gcc and libs
# - git
# - make
RUN apk update && \
    apk add --no-cache gcc && \
    apk add --no-cache musl-dev && \
    apk add --no-cache git && \
    apk add --no-cache make

# Build ntttcp
RUN git clone https://github.com/Microsoft/ntttcp-for-linux
WORKDIR "ntttcp-for-linux/src"
RUN make && make install

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
FROM mcr.microsoft.com/dotnet/aspnet:6.0.3-alpine3.15-amd64 as final
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 80
EXPOSE 443
# Ports for iperf3
EXPOSE 5201/tcp 5201/udp
# Port for qperf
EXPOSE 19765/tcp

# Install to final release image
# - iperf3
# - qperf
# - globalization related lib
RUN apk update && \
    apk add --no-cache iperf3 && \
    apk add --no-cache --repository http://dl-3.alpinelinux.org/alpine/edge/testing/ qperf==0.4.11-r0 && \
    apk add --no-cache icu-libs
WORKDIR /app

# Copy content from Build image
COPY --from=build /app/publish .
COPY --from=build ntttcp-for-linux/src/ntttcp /usr/bin
ENTRYPOINT ["dotnet", "WebApp.dll"]
