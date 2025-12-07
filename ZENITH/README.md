# ZENITH - E-Commerce Platform

ZENITH là một nền tảng thương mại điện tử chuyên về đồ thể thao dành cho sinh viên Việt Nam.

## Yêu Cầu Hệ Thống

- **.NET 9.0 SDK** hoặc cao hơn
- **SQL Server LocalDB** hoặc **SQL Server Express/Full** (2019 trở lên)
- **Visual Studio 2022** hoặc **Visual Studio Code** (khuyến nghị)
- **Git** (để clone repository)

## Cài Đặt

### 1. Cài Đặt .NET 9.0 SDK

Tải và cài đặt .NET 9.0 SDK từ: https://dotnet.microsoft.com/download/dotnet/9.0

### 2. Cài Đặt SQL Server LocalDB

#### Windows:
- SQL Server LocalDB thường được cài đặt cùng với Visual Studio
- Hoặc tải từ: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- Chọn "Express" edition và đảm bảo chọn "LocalDB"

#### Kiểm tra LocalDB đã cài đặt:
```powershell
sqllocaldb info
```

Nếu chưa có, cài đặt SQL Server Express với LocalDB.

### 3. Clone Repository

```bash
git clone <repository-url>
cd ZENITH
```

### 4. Cấu Hình Connection String

Mở file `appsettings.json` và kiểm tra connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ZenithDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**Lưu ý:** 
- Nếu sử dụng SQL Server Express thay vì LocalDB, thay đổi connection string:
  ```
  Server=localhost\\SQLEXPRESS;Database=ZenithDB;Trusted_Connection=True;MultipleActiveResultSets=true
  ```
- Nếu sử dụng SQL Server với username/password:
  ```
  Server=localhost;Database=ZenithDB;User Id=sa;Password=YourPassword;Trusted_Connection=False;MultipleActiveResultSets=true
  ```

### 5. Restore Dependencies

```bash
dotnet restore
```

### 6. Chạy Migrations và Seed Data

Ứng dụng sẽ tự động:
- Tạo database nếu chưa tồn tại
- Chạy migrations
- Seed dữ liệu mẫu (sản phẩm, categories, sports, admin user)

### 7. Chạy Ứng Dụng

```bash
dotnet run
```

Hoặc sử dụng Visual Studio:
- Nhấn F5 hoặc chọn "Run" từ menu

Ứng dụng sẽ chạy tại:
- HTTP: `http://localhost:5191`
- HTTPS: `https://localhost:7265`

## Tài Khoản Mặc Định

Sau khi seed dữ liệu, bạn có thể đăng nhập với tài khoản admin:

- **Email:** `admin@zenith.com`
- **Password:** `Admin@123`

## Cấu Trúc Dự Án

```
ZENITH/
├── AppData/              # Database files và DbInitializer
├── Areas/                # Identity area
├── Controllers/          # MVC Controllers
├── Models/               # Entity models
├── Services/             # Business logic services
├── ViewComponents/        # View Components
├── Views/                # Razor views
├── wwwroot/              # Static files (CSS, JS, images)
├── Migrations/           # Entity Framework migrations
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration
```

## Troubleshooting

### Lỗi: "Cannot connect to database"

**Giải pháp:**
1. Kiểm tra SQL Server LocalDB đã được cài đặt:
   ```powershell
   sqllocaldb info
   ```
2. Khởi động LocalDB instance:
   ```powershell
   sqllocaldb start mssqllocaldb
   ```
3. Kiểm tra connection string trong `appsettings.json`

### Lỗi: "Database already exists"

**Giải pháp:**
1. Xóa database cũ:
   ```sql
   DROP DATABASE [ZenithDB_AppData];
   ```
2. Hoặc xóa file database trong thư mục `AppData/`:
   - `ZenithDB.mdf`
   - `ZenithDB_log.ldf`

### Lỗi: "Port already in use"

**Giải pháp:**
1. Thay đổi port trong `Properties/launchSettings.json`
2. Hoặc dừng ứng dụng đang chạy trên port đó

### Lỗi: "Package restore failed"

**Giải pháp:**
1. Xóa thư mục `bin/` và `obj/`
2. Chạy lại:
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

## Phát Triển

### Thêm Migration Mới

```bash
dotnet ef migrations add <MigrationName>
```

### Cập Nhật Database

```bash
dotnet ef database update
```

### Xóa Migration

```bash
dotnet ef migrations remove
```

## License

Copyright © 2025 ZENITH. All rights reserved.

