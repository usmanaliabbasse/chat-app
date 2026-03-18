# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ChatSupportApi.csproj", "./"]
COPY ["NuGet.config", "./"]
RUN dotnet restore --configfile NuGet.config

# Copy everything else and build
COPY . .
RUN dotnet build "ChatSupportApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ChatSupportApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatSupportApi.dll"]
