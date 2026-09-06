#!/bin/bash
set -e

# Default platform
PLATFORM="linux/amd64"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case "$1" in
    --platform)
      PLATFORM="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [--platform linux/amd64|linux/arm64]"
      exit 1
      ;;
  esac
done

echo "Building for platform: $PLATFORM"
docker buildx build -t huajibot --platform "$PLATFORM" --load .
docker save -o huajibot.tar huajibot
