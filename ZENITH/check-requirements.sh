#!/bin/bash
# Script kiểm tra yêu cầu hệ thống cho ZENITH (Linux/macOS)

echo "========================================"
echo "  ZENITH - Kiểm Tra Yêu Cầu Hệ Thống"
echo "========================================"
echo ""

all_good=true

# Kiểm tra .NET SDK
echo "[1/3] Kiểm tra .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    echo "  ✓ .NET SDK: $dotnet_version"
    
    # Kiểm tra phiên bản
    major_version=$(echo $dotnet_version | cut -d. -f1)
    if [ "$major_version" -lt 9 ]; then
        echo "  ⚠ Cảnh báo: Cần .NET 9.0 trở lên. Phiên bản hiện tại: $dotnet_version"
        all_good=false
    fi
else
    echo "  ✗ .NET SDK chưa được cài đặt!"
    echo "    Tải từ: https://dotnet.microsoft.com/download/dotnet/9.0"
    all_good=false
fi

echo ""

# Kiểm tra SQL Server (Linux/macOS sử dụng SQL Server hoặc SQLite)
echo "[2/3] Kiểm tra SQL Server..."
echo "  ℹ Lưu ý: Trên Linux/macOS, bạn cần cấu hình SQL Server hoặc thay đổi sang SQLite"
echo "    Xem hướng dẫn trong README.md"

echo ""

# Kiểm tra file cấu hình
echo "[3/3] Kiểm tra file cấu hình..."
if [ -f "appsettings.json" ]; then
    echo "  ✓ appsettings.json tồn tại"
    
    if grep -q "DefaultConnection" appsettings.json; then
        echo "  ✓ Connection string đã được cấu hình"
    else
        echo "  ✗ Connection string chưa được cấu hình!"
        all_good=false
    fi
else
    echo "  ✗ appsettings.json không tồn tại!"
    all_good=false
fi

echo ""
echo "========================================"

if [ "$all_good" = true ]; then
    echo "  ✓ Tất cả yêu cầu đã được đáp ứng!"
    echo "  Bạn có thể chạy: dotnet run"
else
    echo "  ⚠ Một số yêu cầu chưa được đáp ứng"
    echo "  Vui lòng xem hướng dẫn trong README.md hoặc SETUP.md"
fi

echo "========================================"

