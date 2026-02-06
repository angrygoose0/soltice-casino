#!/bin/bash

# Solstice Casino WebGL Deploy Script
# Uploads build files to Cloudflare R2

set -e

# ============================================
# CONFIGURATION - Update these values
# ============================================
R2_BUCKET="solstice-casino"
R2_ACCOUNT_ID="your-account-id"  # Find in Cloudflare Dashboard URL
BUILD_DIR="./WebGLBuild"         # Path to your Unity WebGL build output

# Your R2 public URL (after enabling public access)
# Format: https://pub-{hash}.r2.dev or custom domain
R2_PUBLIC_URL="https://${R2_BUCKET}.${R2_ACCOUNT_ID}.r2.cloudflarestorage.com"

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
wrangler r2 object put "${R2_BUCKET}/Build" --file="${BUILD_DIR}/Build" --recursive 2>/dev/null || {
    # Fallback: upload files individually
    for file in "${BUILD_DIR}/Build"/*; do
        if [ -f "$file" ]; then
            filename=$(basename "$file")
            echo "   Uploading: $filename"
            wrangler r2 object put "${R2_BUCKET}/Build/${filename}" --file="$file"
        fi
    done
}

# Upload StreamingAssets if exists
if [ -d "${BUILD_DIR}/StreamingAssets" ]; then
    echo "📦 Uploading StreamingAssets..."
    for file in "${BUILD_DIR}/StreamingAssets"/*; do
        if [ -f "$file" ]; then
            filename=$(basename "$file")
            echo "   Uploading: $filename"
            wrangler r2 object put "${R2_BUCKET}/StreamingAssets/${filename}" --file="$file"
        fi
    done
fi

# Upload index.html and other root files
echo "📄 Uploading HTML files..."
for file in "${BUILD_DIR}"/*.html "${BUILD_DIR}"/*.ico "${BUILD_DIR}"/*.json; do
    if [ -f "$file" ]; then
        filename=$(basename "$file")
        echo "   Uploading: $filename"
        wrangler r2 object put "${R2_BUCKET}/${filename}" --file="$file"
    fi
done

echo ""
echo "✅ Deploy complete!"
echo ""
echo "🌐 Your game should be available at:"
echo "   ${R2_PUBLIC_URL}/index.html"
echo ""
echo "📝 Next steps:"
echo "   1. Enable public access on R2 bucket if not done"
echo "   2. Optionally set up a custom domain in R2 settings"
