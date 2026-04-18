FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY ["LinnetServer/LinnetServer.csproj", "LinnetServer/"]
RUN dotnet restore "LinnetServer/LinnetServer.csproj"
COPY . .
WORKDIR "/src/LinnetServer"
RUN dotnet build "./LinnetServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LinnetServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
RUN mkdir -p /app/wwwroot/logos && chown -R $APP_UID /app/wwwroot/logos
USER $APP_UID
ENTRYPOINT ["dotnet", "LinnetServer.dll"]
