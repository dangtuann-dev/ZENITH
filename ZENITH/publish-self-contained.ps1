# Script PowerShell để publish dự án ZENITH ở chế độ Self-Contained
# Sử dụng: .\publish-self-contained.ps1

Write-Host "🚀 Bắt đầu publish dự án ZENITH ở chế độ Self-Contained..." -ForegroundColor Green

# Tham số mặc định
$Configuration = "Release"
$RuntimeIdentifier = "win-x64"
$OutputPath = "bin\Release\net9.0\win-x64\publish"

# Kiểm tra xem có tham số tùy chỉnh không
param(
    [string]$Runtime = "win-x64",
    [string]$Config = "Release",
    [string]$Output = ""
)

if ($Runtime) {
    $RuntimeIdentifier = $Runtime
}

if ($Config) {
    $Configuration = $Config
}

if ($Output) {
    $OutputPath = $Output
}

Write-Host "📋 Cấu hình:" -ForegroundColor Cyan
Write-Host "   - Configuration: $Configuration" -ForegroundColor White
Write-Host "   - Runtime Identifier: $RuntimeIdentifier" -ForegroundColor White
Write-Host "   - Self-Contained: true" -ForegroundColor White
Write-Host "   - PublishSingleFile: true" -ForegroundColor White
Write-Host "   - Output Path: $OutputPath" -ForegroundColor White
Write-Host ""

# Xây dựng lệnh publish
$publishCommand = "dotnet publish -c $Configuration -r $RuntimeIdentifier --self-contained true /p:PublishSingleFile=true"

if ($Output) {
    $publishCommand += " -o `"$Output`""
}

Write-Host "🔨 Đang thực thi lệnh publish..." -ForegroundColor Yellow
Write-Host "   $publishCommand" -ForegroundColor Gray
Write-Host ""

# Thực thi lệnh
try {
    Invoke-Expression $publishCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Publish thành công!" -ForegroundColor Green
        Write-Host "📦 Các file đã được đóng gói tại: $OutputPath" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "💡 Lưu ý:" -ForegroundColor Yellow
        Write-Host "   - File thực thi chính: ZENITH.exe" -ForegroundColor White
        Write-Host "   - Tất cả dependencies (bao gồm .NET Runtime) đã được đóng gói" -ForegroundColor White
        Write-Host "   - Bạn có thể chạy ứng dụng trên bất kỳ máy Windows nào mà không cần cài .NET Runtime" -ForegroundColor White
    } else {
        Write-Host ""
        Write-Host "❌ Publish thất bại với mã lỗi: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "❌ Lỗi khi thực thi publish: $_" -ForegroundColor Red
    exit 1
}
