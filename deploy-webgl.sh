#!/bin/bash

# Solstice Casino WebGL Deploy Script
# Uploads build files to Cloudflare R2

set -e

# ============================================
# CONFIGURATION - Update these values
# ============================================
R2_BUCKET="solstice-casino"
BUILD_DIR="./build"              # Path to your Unity WebGL build output
PUBLIC_URL="https://app.soltice.fun"

# ============================================

echo "🎰 Deploying Solstice Casino WebGL Build"
echo "========================================="

# Check if build directory exists
if [ ! -d "$BUILD_DIR" ]; then
    echo "❌ Build directory not found: $BUILD_DIR"
    echo "   Run Unity WebGL build first, then update BUILD_DIR path"
    exit 1
fi

# Check if wrangler is installed
if ! command -v wrangler &> /dev/null; then
    echo "❌ wrangler CLI not found. Install with: npm install -g wrangler"
    exit 1
fi

echo "📦 Uploading build files to R2..."

# Upload all files from Build folder
for file in "${BUILD_DIR}/Build"/*; do
    if [ -f "$file" ]; then
        filename=$(basename "$file")
        echo "   Uploading: Build/$filename"
        wrangler r2 object put "${R2_BUCKET}/Build/${filename}" --file="$file" --remote
    fi
done

# Upload StreamingAssets if exists
if [ -d "${BUILD_DIR}/StreamingAssets" ]; then
    echo "📦 Uploading StreamingAssets..."
    for file in "${BUILD_DIR}/StreamingAssets"/*; do
        if [ -f "$file" ]; then
            filename=$(basename "$file")
            echo "   Uploading: $filename"
            wrangler r2 object put "${R2_BUCKET}/StreamingAssets/${filename}" --file="$file" --remote
        fi
    done
fi

# Upload index.html and other root files
echo "📄 Uploading HTML files..."
for file in "${BUILD_DIR}"/*.html "${BUILD_DIR}"/*.ico "${BUILD_DIR}"/*.json; do
    if [ -f "$file" ]; then
        filename=$(basename "$file")
        echo "   Uploading: $filename"
        wrangler r2 object put "${R2_BUCKET}/${filename}" --file="$file" --remote
    fi
done

echo ""
echo "✅ Deploy complete!"
echo ""
echo "🌐 Your game should be available at:"
echo "   ${PUBLIC_URL}"
