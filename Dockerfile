# Cloudativ Assessment - Dockerfile
# Multi-stage build for optimized image size

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CloudativAssessment.sln", "./"]
COPY ["src/Cloudativ.Assessment.Domain/Cloudativ.Assessment.Domain.csproj", "src/Cloudativ.Assessment.Domain/"]
COPY ["src/Cloudativ.Assessment.Application/Cloudativ.Assessment.Application.csproj", "src/Cloudativ.Assessment.Application/"]
COPY ["src/Cloudativ.Assessment.Infrastructure/Cloudativ.Assessment.Infrastructure.csproj", "src/Cloudativ.Assessment.Infrastructure/"]
COPY ["src/Cloudativ.Assessment.Web/Cloudativ.Assessment.Web.csproj", "src/Cloudativ.Assessment.Web/"]

# Restore dependencies
RUN dotnet restore "src/Cloudativ.Assessment.Web/Cloudativ.Assessment.Web.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Cloudativ.Assessment.Web"
RUN dotnet build "Cloudativ.Assessment.Web.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Cloudativ.Assessment.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs && chown -R appuser:appuser /app

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/cloudativ_assessment.db"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Cloudativ.Assessment.Web.dll"]
