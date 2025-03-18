#!/usr/bin/env bash

# -------------------------------------------------------------------
# generate-dockerfile.sh
#
# Generates a Dockerfile for building and running a .NET app
# in a monorepo, copying the .NET solution and the necessary app
# including any referenced packages (shared libraries).
#
# Requirements/Assumptions:
#   - Deployable apps are in ./apps/<AppName>/
#   - Shared libs are in ./packages/<SomeLibrary>/
#   - The <ProjectReference> in the .csproj points to the packages folder
#     using relative paths like: ..\..\packages\SomeLibrary\SomeLibrary.csproj
#   - This script parses those references to selectively COPY only required
#     folders into the Docker build context.
#
# Usage:
#   ./generate-dockerfile.sh <CsProjFilePath>
# Example:
#   ./generate-dockerfile.sh apps/sample-microservice/src/SampleMicroservice.Presentation/SampleMicroservice.Presentation.csproj
# CsProjFilePath:
#   The path to the .csproj file of the app you want to build. The project MUST contain Program.cs.
# -------------------------------------------------------------------

set -e

# Ensure we have an argument
if [ -z "$1" ]; then
  echo "ERROR: You must provide the path to the .csproj file of the app you want to build."
  echo "Usage: $0 <CsProjFilePath>"
  exit 1
fi

APP_CSPROJ_FILE="$1"
CS_PROJ_PATH=$APP_CSPROJ_FILE
CS_PROJ_DIR=$(dirname "${CS_PROJ_PATH}")
CS_PROJ_NAME=$(basename "${CS_PROJ_PATH}")
SLN_DIR=$(dirname $(find . -name "*.sln" | head -1))

# Read .NET version from the TargetFramework in the .csproj
NET_VERSION=$(grep -oE '<TargetFramework>[^<]+' "${CS_PROJ_PATH}" \
             | sed -E 's|<TargetFramework>(net*)|\1|' \
             | head -1 \
             | grep -oE '[0-9]+\.[0-9]+')

if [ -z "$NET_VERSION" ]; then
  # Fallback to .NET 7.0 if not found
  NET_VERSION="7.0"
fi

# Extract the major version number
MAJOR_VERSION=$(echo "$NET_VERSION" | cut -d '.' -f1)

# Decide the port based on major version
if [ "$MAJOR_VERSION" -lt 8 ]; then
 ASPNET_CORE_PORT=80
else
 ASPNET_CORE_PORT=8080
fi

# Parse assembly name or namespace from the .csproj
ASSEMBLY_NAME=$(grep -oE '<AssemblyName>[^<]+' "${CS_PROJ_PATH}" \
               | sed -E 's|<AssemblyName>(.*)|\1|' \
               | head -1)
if [ -z "$ASSEMBLY_NAME" ]; then
  # If no <AssemblyName>, try <RootNamespace>
  ASSEMBLY_NAME=$(grep -oE '<RootNamespace>[^<]+' "${CS_PROJ_PATH}" \
                 | sed -E 's|<RootNamespace>(.*)|\1|' \
                 | head -1)
fi

# Make sure the .csproj exists
if [ ! -f "${CS_PROJ_PATH}" ]; then
  echo "ERROR: Could not find ${CS_PROJ_PATH}"
  exit 1
fi

#
# 1. Parse all <ProjectReference Include="..."> lines from the app's .csproj
#    that point to something under packages/.
#
#    We’ll extract the relative paths so we know which package folders are needed.
#    Note: This is a simplistic regex approach; adapt as needed for more complex scenarios.
#

# Use grep to find lines containing ProjectReference, then sed to extract the path
# Use grep + sed instead of grep -oP
REFERENCED_PACKAGE_PATHS=$(
  grep '<ProjectReference Include="' "${CS_PROJ_PATH}" \
    | sed -E 's/.*Include="([^"]+)".*/\1/' \
    | sed 's|\\|/|g' \
    || true
)

# The above may return lines like:
#   ../../packages/SharedLib/SharedLib.csproj
#   ../../packages/AnotherLib/AnotherLib.csproj
#
# We’ll convert these into Docker COPY commands. We need the top-level directory:
#   packages/SharedLib
#   packages/AnotherLib
#

# Now we transform the relative path into a clean directory path (e.g., packages/SharedLib).
# We’ll store those unique directories in an array.
PKG_DIRS_LIST=()
APP_DIRS_LIST=()

for ref in ${REFERENCED_PACKAGE_PATHS}; do
  echo "Found referenced package: $ref"
  if [[ "$ref" == *"packages/"* ]]; then
    PKG_DIR="packages/$(echo "$ref" | sed -E 's/.*packages\///; s/\/[^/]+\.csproj//')"
    # Check if PKG_DIR is already in PKG_DIRS_LIST
    if [[ ! " ${PKG_DIRS_LIST[@]} " =~ " ${PKG_DIR} " ]]; then
      PKG_DIRS_LIST+=("$PKG_DIR")
    fi
  elif [[ "$ref" == *"../"* ]]; then
    # referencing a sibling folder: ../SiblingLib/SiblingLib.csproj
    # We'll treat this as "one level up" from the directory containing $CS_PROJ_PATH
    # so let's figure out the actual path in the monorepo.

    # Step 1: get the folder containing the main .csproj
    main_project_dir=$(dirname "${CS_PROJ_PATH}")  # e.g. apps/MyApp/src/SampleMicroservice.Presentation

    # Step 2: combine with the relative reference
    # We'll turn it into a host-relative path:
    sibling_path="${main_project_dir}/${ref}"  # e.g. apps/MyApp/src/SampleMicroservice.Presentation/../SiblingLib/SiblingLib.csproj

    # Step 3: remove the final "/Something.csproj" portion to get the directory
    sibling_dir=$(dirname "$sibling_path")  # e.g. apps/MyApp/src/SampleMicroservice.Presentation/../SiblingLib

    # (Optional) you might want to "normalize" that path if your shell supports realpath:
    # sibling_dir=$(realpath "$sibling_dir")  # e.g. /abs/path/to/repo/apps/MyApp/src/SiblingLib

    # But Docker COPY expects a relative path inside the build context
    # so we want a path like apps/MyApp/src/SiblingLib
    # We'll convert it back to relative from the repo root:
    # (If you rely on realpath, you'd have to transform again or just keep it relative.)
    # One naive approach: remove the leading absolute part if you know the repo root.

    # If we assume this script runs at the repo root, we can do something like:
    sibling_dir_rel=$(echo "$sibling_dir" | sed 's|^\./||')  # remove ./ if present

    # Store it if not already in array
    [[ " ${APP_DIRS_LIST[*]} " != *" $sibling_dir_rel "* ]] && APP_DIRS_LIST+=("$sibling_dir_rel")
  fi
done

echo "Referenced packages: ${PKG_DIRS_LIST[*]}"
echo "Referenced apps: ${APP_DIRS_LIST[*]}"

#
# 2. Generate the Dockerfile content. We'll do a multi-stage build:
#    - Stage 1: Build
#    - Stage 2: Runtime
#
#    We will do selective COPY instructions:
#      - Copy .sln (optional but typical if you restore from the solution).
#      - Copy the single app .csproj
#      - Copy each required package’s .csproj
#      - dotnet restore
#      - Then copy the full source of the app & required packages
#      - dotnet publish
#      - In the final stage, copy the published output
#

DOCKERFILE_CONTENT=$(cat <<EOF
# ------------------------------------------------------------
# STAGE 0: BASE
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${NET_VERSION} AS base
WORKDIR "/src"
EXPOSE ${ASPNET_CORE_PORT}

# ------------------------------------------------------------
# STAGE 1: BUILD
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${NET_VERSION} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src"

# 1) Copy the app's .csproj
COPY ["${CS_PROJ_PATH}", "${CS_PROJ_DIR}/"]

EOF
)

#
# 2a. Add lines to copy each referenced package’s .csproj
#
for pkg_dir in "${PKG_DIRS_LIST[@]}"; do
  # We assume the .csproj file has the same name as the folder, or you can adapt.
  # Let's find all .csproj files in that package folder (in case there's just one).
  # We'll do it in a naive way by pattern, or you can guess the name matches the dir.
  PKG_CSPROJ=$(find "${pkg_dir}" -maxdepth 1 -name "*.csproj" 2>/dev/null | head -1)
  if [ -n "${PKG_CSPROJ}" ]; then
    # We want to replicate the same structure in the container, so:
    # e.g. COPY packages/SharedLib/SharedLib.csproj packages/SharedLib/
DOCKERFILE_CONTENT+=$(cat <<EOF 

COPY ["${PKG_CSPROJ}", "../../${pkg_dir}"]

EOF
)
  fi
done

for app_dir in "${APP_DIRS_LIST[@]}"; do
  # We assume the .csproj file has the same name as the folder, or you can adapt.
  # Let's find all .csproj files in that package folder (in case there's just one).
  # We'll do it in a naive way by pattern, or you can guess the name matches the dir.
  APP_CSPROJ=$(find "${app_dir}" -maxdepth 1 -name "*.csproj" 2>/dev/null | head -1)
  if [ -n "${APP_CSPROJ}" ]; then
    # We want to replicate the same structure in the container, so:
    # e.g. COPY apps/MyMicroservice/MyMicroservice.csproj apps/MyMicroservice/
DOCKERFILE_CONTENT+=$(cat <<EOF

COPY ["${APP_CSPROJ}", "${app_dir}"]

EOF
)
  fi
done


#
# 2b. Restore
#
DOCKERFILE_CONTENT+=$(
cat <<EOF


# 2) Restore
RUN dotnet restore "${CS_PROJ_PATH}"

# 3) Now copy the full source for the app
WORKDIR "/src"
COPY ["${CS_PROJ_DIR}/", "${CS_PROJ_DIR}/"]
EOF
)

#
# 2c. Copy the full source for each needed package
#
for pkg_dir in "${PKG_DIRS_LIST[@]}"; do
DOCKERFILE_CONTENT+=$(
cat <<EOF

COPY ["${pkg_dir}/", "${pkg_dir}/"]

EOF
)
done

for app_dir in "${APP_DIRS_LIST[@]}"; do
  # We assume the .csproj file has the same name as the folder, or you can adapt.
  # Let's find all .csproj files in that package folder (in case there's just one).
  # We'll do it in a naive way by pattern, or you can guess the name matches the dir.
  APP_CSPROJ=$(find "${app_dir}" -maxdepth 1 -name "*.csproj" 2>/dev/null | head -1)
  if [ -n "${APP_CSPROJ}" ]; then
    # We want to replicate the same structure in the container, so:
    # e.g. COPY apps/MyMicroservice/MyMicroservice.csproj apps/MyMicroservice/
DOCKERFILE_CONTENT+=$(cat <<EOF

COPY ["${APP_CSPROJ}", "${app_dir}"]

EOF
)
  fi
done

DOCKERFILE_CONTENT+=$(
cat <<EOF


# 4) Build
WORKDIR "${CS_PROJ_DIR}"
RUN dotnet build "${CS_PROJ_NAME}" -c \$BUILD_CONFIGURATION -o /app/build

EOF
)

#
# 2d. Publish
#
DOCKERFILE_CONTENT+=$(
cat <<EOF


# 5) Publish the app in Release mode
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "${CS_PROJ_NAME}" -c \$BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------
# STAGE 2: FINAL
# ------------------------------------------------------------
FROM base AS final
WORKDIR "/app"

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "${ASSEMBLY_NAME}.dll"]
EOF
)

#
# 3. Write the final Dockerfile
#
echo "${DOCKERFILE_CONTENT}" > "${SLN_DIR}/Dockerfile"

echo "Dockerfile generated successfully for ${APP_NAME}."
echo "Referenced packages (if any): ${PKG_DIRS_LIST[*]}"
echo "You can now build the image with: docker build -t ${APP_NAME}-image ."