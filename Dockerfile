# ==============================================================================
# Multi-stage Dockerfile for Clinic Appointment Booking System (.NET 10)
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project definitions for optimized Docker layer caching
COPY ClinicBooking.slnx ./
COPY Directory.Build.props ./
COPY src/ClinicBooking.Domain/*.csproj ./src/ClinicBooking.Domain/
COPY src/ClinicBooking.Application/*.csproj ./src/ClinicBooking.Application/
COPY src/ClinicBooking.Infrastructure/*.csproj ./src/ClinicBooking.Infrastructure/
COPY src/ClinicBooking.API/*.csproj ./src/ClinicBooking.API/
COPY tests/ClinicBooking.Domain.Tests/*.csproj ./tests/ClinicBooking.Domain.Tests/
COPY tests/ClinicBooking.Application.Tests/*.csproj ./tests/ClinicBooking.Application.Tests/
COPY tests/ClinicBooking.Infrastructure.Tests/*.csproj ./tests/ClinicBooking.Infrastructure.Tests/
COPY tests/ClinicBooking.API.Tests/*.csproj ./tests/ClinicBooking.API.Tests/

# Restore dependencies
RUN dotnet restore ClinicBooking.slnx

# Copy the remaining source tree
COPY . .

# Run tests during image build to guarantee artifact integrity
RUN dotnet test --no-restore -c Release --filter "FullyQualifiedName!~Integration"

# Publish the Web API
WORKDIR /src/src/ClinicBooking.API
RUN dotnet publish ClinicBooking.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as non-root user (app is provided by ASP.NET Core base images)
USER $APP_UID

# Expose standard container ports
EXPOSE 8080
EXPOSE 8081

# Copy compiled binaries from build stage
COPY --from=build /app/publish .

# Set default ASP.NET Core hosting environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "ClinicBooking.API.dll"]
