#!/bin/bash
# Build script for boilerplate-cli-ui-dotnet

set -e

APP_NAME="boilerplate-cli-ui-dotnet"

echo "Building ${APP_NAME}..."

# Build self-contained single binary
dotnet publish -c Release -r linux-x64 -o ./dist/linux

echo "Built: ./dist/linux/${APP_NAME}"
ls -lh ./dist/linux/${APP_NAME}

echo ""
echo "Cross-compile for other platforms:"
echo "  macOS ARM64:  dotnet publish -c Release -r osx-arm64 -o ./dist/mac"
echo "  Windows:      dotnet publish -c Release -r win-x64 -o ./dist/win"
echo ""
echo "Usage:"
echo "  ./dist/linux/${APP_NAME} start           # Start server with UI"
echo "  ./dist/linux/${APP_NAME} start -p 3000   # Start on custom port"
echo "  ./dist/linux/${APP_NAME} version         # Show version"
