# Hướng Dẫn Setup Nhanh

## Bước 1: Kiểm Tra Yêu Cầu

### Kiểm tra .NET SDK:
```bash
dotnet --version
```
Cần phiên bản 9.0 trở lên.

### Kiểm tra SQL Server LocalDB:
```powershell
sqllocaldb info
```

Nếu chưa có, cài đặt SQL Server Express với LocalDB từ: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

## Bước 2: Clone và Restore

```bash
# Clone repository (nếu chưa có)
git clone <repository-url>
cd ZENITH

# Restore packages
dotnet restore
```

## Bước 3: Cấu Hình Database

### Option 1: Sử dụng LocalDB (Mặc định - Khuyến nghị)

Không cần thay đổi gì, connection string trong `appsettings.json` đã được cấu hình sẵn:
```
Server=(localdb)\mssqllocaldb;Database=ZenithDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

### Option 2: Sử dụng SQL Server Express

Sửa `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ZenithDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Option 3: Sử dụng SQL Server với Username/Password

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ZenithDB;User Id=sa;Password=YourPassword;Trusted_Connection=False;MultipleActiveResultSets=true"
  }
}
```

## Bước 4: Chạy Ứng Dụng

```bash
dotnet run
```

Ứng dụng sẽ tự động:
- ✅ Tạo database nếu chưa có
- ✅ Chạy migrations
- ✅ Seed dữ liệu mẫu

## Bước 5: Truy Cập Ứng Dụng

Mở trình duyệt và truy cập:
- HTTP: `http://localhost:5191`
- HTTPS: `https://localhost:7265`

## Đăng Nhập

Sau khi seed dữ liệu, đăng nhập với:
- **Email:** `admin@zenith.com`
- **Password:** `Admin@123`

## Xử Lý Lỗi Thường Gặp

### ❌ "Cannot connect to database"

**Giải pháp:**
```powershell
# Khởi động LocalDB
sqllocaldb start mssqllocaldb

# Kiểm tra trạng thái
sqllocaldb info mssqllocaldb
```

### ❌ "Database already exists"

**Giải pháp:**
```sql
-- Chạy trong SQL Server Management Studio hoặc sqlcmd
DROP DATABASE [ZenithDB_AppData];
```

Hoặc xóa file trong `AppData/`:
- `ZenithDB.mdf`
- `ZenithDB_log.ldf`

### ❌ "Port already in use"

**Giải pháp:**
Sửa port trong `Properties/launchSettings.json`:
```json
{
  "applicationUrl": "http://localhost:5000;https://localhost:5001"
}
```

## Lưu Ý Quan Trọng

1. **Database files** (`.mdf`, `.ldf`) nằm trong thư mục `AppData/` - không commit vào Git
2. **User secrets** được lưu trong `secrets.json` - không commit vào Git
3. **Connection string** có thể override bằng environment variable:
   ```bash
   set ConnectionStrings__DefaultConnection="YourConnectionString"
   ```

## Hỗ Trợ

Nếu gặp vấn đề, vui lòng kiểm tra:
1. `.NET SDK` đã cài đặt đúng phiên bản
2. `SQL Server LocalDB` đã được cài đặt và khởi động
3. `Connection string` trong `appsettings.json` đúng
4. Port không bị conflict với ứng dụng khác

