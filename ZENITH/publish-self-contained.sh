#!/bin/bash
# Script Bash để publish dự án ZENITH ở chế độ Self-Contained
# Sử dụng: ./publish-self-contained.sh

echo "🚀 Bắt đầu publish dự án ZENITH ở chế độ Self-Contained..."

# Tham số mặc định
CONFIGURATION="Release"
RUNTIME_IDENTIFIER="win-x64"
OUTPUT_PATH="bin/Release/net9.0/win-x64/publish"

# Kiểm tra tham số
if [ ! -z "$1" ]; then
    RUNTIME_IDENTIFIER="$1"
fi

if [ ! -z "$2" ]; then
    CONFIGURATION="$2"
fi

if [ ! -z "$3" ]; then
    OUTPUT_PATH="$3"
fi

echo "📋 Cấu hình:"
echo "   - Configuration: $CONFIGURATION"
echo "   - Runtime Identifier: $RUNTIME_IDENTIFIER"
echo "   - Self-Contained: true"
echo "   - PublishSingleFile: true"
echo "   - Output Path: $OUTPUT_PATH"
echo ""

# Xây dựng lệnh publish
PUBLISH_CMD="dotnet publish -c $CONFIGURATION -r $RUNTIME_IDENTIFIER --self-contained true /p:PublishSingleFile=true"

if [ ! -z "$3" ]; then
    PUBLISH_CMD="$PUBLISH_CMD -o \"$OUTPUT_PATH\""
fi

echo "🔨 Đang thực thi lệnh publish..."
echo "   $PUBLISH_CMD"
echo ""

# Thực thi lệnh
if eval $PUBLISH_CMD; then
    echo ""
    echo "✅ Publish thành công!"
    echo "📦 Các file đã được đóng gói tại: $OUTPUT_PATH"
    echo ""
    echo "💡 Lưu ý:"
    echo "   - File thực thi chính: ZENITH.exe"
    echo "   - Tất cả dependencies (bao gồm .NET Runtime) đã được đóng gói"
    echo "   - Bạn có thể chạy ứng dụng trên bất kỳ máy Windows nào mà không cần cài .NET Runtime"
else
    echo ""
    echo "❌ Publish thất bại!"
    exit 1
fi
