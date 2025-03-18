# ------------------------------------------------------------
# STAGE 0: BASE
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR "/src"
EXPOSE 8080

# ------------------------------------------------------------
# STAGE 1: BUILD
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src"

# 1) Copy the app's .csproj
COPY ["EmailCleaner.API/EmailCleaner.API.csproj", "EmailCleaner.API/"]
COPY ["EmailCleaner.API/../EmailCleaner.Domain/EmailCleaner.Domain.csproj", "EmailCleaner.API/../EmailCleaner.Domain"]
COPY ["EmailCleaner.API/../EmailCleaner.Infrastructure/EmailCleaner.Infrastructure.csproj", "EmailCleaner.API/../EmailCleaner.Infrastructure"]
COPY ["EmailCleaner.API/../../SharedKernel/SharedKernel.csproj", "EmailCleaner.API/../../SharedKernel"]

# 2) Restore
RUN dotnet restore "EmailCleaner.API/EmailCleaner.API.csproj"

# 3) Now copy the full source for the app
WORKDIR "/src"
COPY ["EmailCleaner.API/", "EmailCleaner.API/"]
COPY ["EmailCleaner.API/../EmailCleaner.Domain/EmailCleaner.Domain.csproj", "EmailCleaner.API/../EmailCleaner.Domain"]
COPY ["EmailCleaner.API/../EmailCleaner.Infrastructure/EmailCleaner.Infrastructure.csproj", "EmailCleaner.API/../EmailCleaner.Infrastructure"]
COPY ["EmailCleaner.API/../../SharedKernel/SharedKernel.csproj", "EmailCleaner.API/../../SharedKernel"]

# 4) Build
WORKDIR "EmailCleaner.API"
RUN dotnet build "EmailCleaner.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 5) Publish the app in Release mode
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EmailCleaner.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------
# STAGE 2: FINAL
# ------------------------------------------------------------
FROM base AS final
WORKDIR "/app"

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", ".dll"]
