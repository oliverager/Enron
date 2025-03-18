# ------------------------------------------------------------
# STAGE 0: BASE
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR "/src"
EXPOSE 8080

# ------------------------------------------------------------
# STAGE 1: BUILD
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src"

# 1) Copy the app's .csproj
COPY ["emailindexer.api/emailindexer.api.csproj", "emailindexer.api/"]
COPY ["emailindexer.api/../EmailIndexer.Domain/EmailIndexer.Domain.csproj", "emailindexer.api/../EmailIndexer.Domain"]
COPY ["emailindexer.api/../EmailIndexer.Infrastructure/EmailIndexer.Infrastructure.csproj", "emailindexer.api/../EmailIndexer.Infrastructure"]
COPY ["emailindexer.api/../../SharedKernel/SharedKernel.csproj", "emailindexer.api/../../SharedKernel"]

# 2) Restore
RUN dotnet restore "emailindexer.api/emailindexer.api.csproj"

# 3) Now copy the full source for the app
WORKDIR "/src"
COPY ["emailindexer.api/", "emailindexer.api/"]
COPY ["emailindexer.api/../EmailIndexer.Domain/EmailIndexer.Domain.csproj", "emailindexer.api/../EmailIndexer.Domain"]
COPY ["emailindexer.api/../EmailIndexer.Infrastructure/EmailIndexer.Infrastructure.csproj", "emailindexer.api/../EmailIndexer.Infrastructure"]
COPY ["emailindexer.api/../../SharedKernel/SharedKernel.csproj", "emailindexer.api/../../SharedKernel"]

# 4) Build
WORKDIR "emailindexer.api"
RUN dotnet build "emailindexer.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 5) Publish the app in Release mode
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "emailindexer.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------
# STAGE 2: FINAL
# ------------------------------------------------------------
FROM base AS final
WORKDIR "/app"

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", ".dll"]
