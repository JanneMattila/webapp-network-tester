# Build image(https://hub.docker.com/_/microsoft-dotnet-core-sdk/)
FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-jammy-amd64 AS build

# Install to build containers
# - gcc and libs
# - git
# - make
#RUN apk update && \
    #apk add --no-cache gcc && \
    #apk add --no-cache musl-dev && \
    #apk add --no-cache git && \
    #apk add --no-cache make
RUN apt-get update && \
    apt-get -y install gcc git make perl make automake autoconf m4 libtool-bin g++

WORKDIR /sources
RUN git clone https://github.com/Microsoft/ntttcp-for-linux
RUN git clone https://github.com/mellanox/sockperf

# Build ntttcp
WORKDIR /sources/ntttcp-for-linux/src
RUN make && make install

# Build sockperf
WORKDIR /sources/sockperf
#RUN apk add --no-cache autoconf
#RUN apk add --no-cache automake
#RUN apk add --no-cache libtool
#RUN apk add --no-cache g++
#RUN apk add --no-cache libexecinfo-dev
RUN ./autogen.sh && \
    ./configure --prefix= && \
    make && \
    make install

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
FROM mcr.microsoft.com/dotnet/aspnet:8.0.0-jammy-amd64 as final
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
# Ports for iperf3
EXPOSE 5201/tcp 5201/udp
# Port for qperf
EXPOSE 19765/tcp

# Install to final release image
# - iperf3
# - qperf
# - globalization related lib
#RUN apk update && \
    #apk add --no-cache iperf3 && \
    #apk add --no-cache --repository http://dl-3.alpinelinux.org/alpine/edge/testing/ qperf==0.4.11-r0 && \
    #apk add --no-cache icu-libs
RUN apt-get update && \
    apt-get install -y iperf3 qperf hping3
WORKDIR /app

# Copy content from Build image
COPY --from=build /app/publish .
COPY --from=build sources/ntttcp-for-linux/src/ntttcp /usr/bin
COPY --from=build sources/sockperf/sockperf /usr/bin
ENTRYPOINT ["dotnet", "WebApp.dll"]
