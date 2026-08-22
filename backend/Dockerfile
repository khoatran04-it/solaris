# ==========================================
# STAGE 1: Build & Restore .NET 8 Web API
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore first for Docker cache optimization
COPY ["backend.csproj", "./"]
RUN dotnet restore "backend.csproj"

# Copy source code and compile
COPY . .
RUN dotnet build "backend.csproj" -c Release -o /app/build

# ==========================================
# STAGE 2: Publish Artifacts
# ==========================================
FROM build AS publish
RUN dotnet publish "backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# STAGE 3: Final Minimal Runtime Image
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Expose HTTP port 8080 (Azure Container Apps / Docker default standard)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published dlls from publish stage
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "backend.dll"]
