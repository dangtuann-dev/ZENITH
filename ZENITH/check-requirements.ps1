# Script kiểm tra yêu cầu hệ thống cho ZENITH
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ZENITH - Kiểm Tra Yêu Cầu Hệ Thống" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# Kiểm tra .NET SDK
Write-Host "[1/3] Kiểm tra .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  ✓ .NET SDK: $dotnetVersion" -ForegroundColor Green
    
    # Kiểm tra phiên bản
    $versionParts = $dotnetVersion.Split('.')
    $majorVersion = [int]$versionParts[0]
    if ($majorVersion -lt 9) {
        Write-Host "  ⚠ Cảnh báo: Cần .NET 9.0 trở lên. Phiên bản hiện tại: $dotnetVersion" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "  ✗ .NET SDK chưa được cài đặt!" -ForegroundColor Red
    Write-Host "    Tải từ: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Gray
    $allGood = $false
}

Write-Host ""

# Kiểm tra SQL Server LocalDB
Write-Host "[2/3] Kiểm tra SQL Server LocalDB..." -ForegroundColor Yellow
try {
    $localdbInfo = sqllocaldb info 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ SQL Server LocalDB đã được cài đặt" -ForegroundColor Green
        
        # Kiểm tra instance mssqllocaldb
        $instances = sqllocaldb info
        if ($instances -match "mssqllocaldb") {
            Write-Host "  ✓ Instance 'mssqllocaldb' đã tồn tại" -ForegroundColor Green
            
            # Kiểm tra trạng thái
            $status = sqllocaldb info mssqllocaldb 2>&1
            if ($status -match "Started") {
                Write-Host "  ✓ Instance 'mssqllocaldb' đang chạy" -ForegroundColor Green
            } else {
                Write-Host "  ⚠ Instance 'mssqllocaldb' chưa khởi động" -ForegroundColor Yellow
                Write-Host "    Chạy: sqllocaldb start mssqllocaldb" -ForegroundColor Gray
            }
        } else {
            Write-Host "  ⚠ Instance 'mssqllocaldb' chưa được tạo" -ForegroundColor Yellow
            Write-Host "    Chạy: sqllocaldb create mssqllocaldb" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ✗ SQL Server LocalDB chưa được cài đặt!" -ForegroundColor Red
        Write-Host "    Tải từ: https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor Gray
        $allGood = $false
    }
} catch {
    Write-Host "  ✗ SQL Server LocalDB chưa được cài đặt!" -ForegroundColor Red
    Write-Host "    Tải từ: https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor Gray
    $allGood = $false
}

Write-Host ""

# Kiểm tra file cấu hình
Write-Host "[3/3] Kiểm tra file cấu hình..." -ForegroundColor Yellow
if (Test-Path "appsettings.json") {
    Write-Host "  ✓ appsettings.json tồn tại" -ForegroundColor Green
    
    $appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
    if ($appsettings.ConnectionStrings.DefaultConnection) {
        Write-Host "  ✓ Connection string đã được cấu hình" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Connection string chưa được cấu hình!" -ForegroundColor Red
        $allGood = $false
    }
} else {
    Write-Host "  ✗ appsettings.json không tồn tại!" -ForegroundColor Red
    $allGood = $false
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($allGood) {
    Write-Host "  ✓ Tất cả yêu cầu đã được đáp ứng!" -ForegroundColor Green
    Write-Host "  Bạn có thể chạy: dotnet run" -ForegroundColor Cyan
} else {
    Write-Host "  ⚠ Một số yêu cầu chưa được đáp ứng" -ForegroundColor Yellow
    Write-Host "  Vui lòng xem hướng dẫn trong README.md hoặc SETUP.md" -ForegroundColor Gray
}

Write-Host "========================================" -ForegroundColor Cyan

