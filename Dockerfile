# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (layer caching)
COPY ["HISYSApplication/HISYSApplication.csproj", "HISYSApplication/"]
RUN dotnet restore "HISYSApplication/HISYSApplication.csproj"

# Copy full source code
COPY . .

# Build and publish release binaries
WORKDIR "/src/HISYSApplication"
RUN dotnet publish "HISYSApplication.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expose default HTTP port
EXPOSE 8080

# Configure ASP.NET Core to listen on port 8080 (or any PORT env variable)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published application from build stage
COPY --from=build /app/publish .

# Start the application
ENTRYPOINT ["dotnet", "HISYSApplication.dll"]
