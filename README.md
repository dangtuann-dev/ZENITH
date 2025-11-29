# ZENITH — Sports E‑Commerce Web Application

ZENITH is a full‑featured ASP.NET Core web application for sports retail. It provides a streamlined shopping experience for customers (browse, search, filter, favorites, cart, checkout, reviews) and complete back‑office tools for administrators (dashboard metrics, product management, orders, users, images).

## Key Features
- Product catalog: list, detail, variant pricing, primary/extra images, rating summary
- Search and filter: keyword search by name/description/SKU; category and sport filters (supporting parent/child hierarchies)
- Favorites: add/remove favorites, recent favorites preview, move/add to cart
- Cart & checkout: quantity updates, variant changes, address management, order placement with stock deduction and sold counts
- Reviews: add/remove, verified purchase flag, average rating
- Profile: personal info, avatar upload, default address, favorites overview
- Admin dashboard: KPIs (users, products, orders, revenue), recent orders, top products, low stock, pending reviews, chart metrics
- Admin product management: list, search, sort, sport‑based category filtering, add/edit/delete products, image management

## Architecture
- Framework: ASP.NET Core 9 (MVC) with Identity (Razor Pages for auth)
- Data: Entity Framework Core (SQL Server / LocalDB). N‑M mapping via `SportCategory`, parent/child trees for `Category` and `Sport`
- Context: `ApplicationDbContext` configures entities, relationships, indices, and Identity table names
- ViewModels: tailored UI models (e.g., `ProductListViewModel`, `ProductDetailViewModel`, `ProductCardViewModel`, checkout/favorites/admin models)
- Client‑side: small AJAX endpoints (e.g., categories by sport) to keep filters responsive
- Performance: eager `Include` where needed, `AsNoTracking` for display, controlled `Take` and pagination

## Project Structure (high level)
- `Controllers/` — MVC controllers (Product, Admin, Favorites, Checkout, Profile, Home)
- `Models/` — EF Core entities (Product, ProductVariant, ProductImage, Category, Sport, SportCategory, Supplier, CartItem, Favorite, Review, Order, OrderItem, Shipment, Voucher, etc.) and Identity types (`ApplicationUser`, `ApplicationRole`)
- `ViewModels/` — data contracts optimized for views (catalog cards, detail, checkout, admin dashboard, menu, etc.)
- `Views/` — Razor views for pages and partials
- `Areas/Identity/Pages/Account/` — Identity UI (Login, Register, etc.) with custom layout
- `AppData/` — `ApplicationDbContext` and `DbInitializer` (database configuration and seed)
- `wwwroot/` — static assets (css/js/images/fonts)
- `Program.cs` — application bootstrap, Identity registration, routing, seeding call

## Getting Started
### Prerequisites
- .NET SDK 9 (or compatible SDK aligned with `TargetFramework`)
- SQL Server LocalDB (default dev setup) or a SQL Server instance

### Configure
- Set the `DefaultConnection` in `appsettings.json`. In dev LocalDB, the app auto‑attaches `AppData/ZenithDB.mdf` and sets an internal catalog

### Build & Run
```bash
dotnet restore
dotnet build
dotnet run
```
- The seeding routine (`DbInitializer`) is invoked before `app.Run()` to bootstrap essential data

### Optional: EF Core Migrations
- Install tools: `dotnet tool install --global dotnet-ef`
- Typical commands:
  - `dotnet ef migrations add <Name>`
  - `dotnet ef database update`

## Notable Implementation Details
- Sport‑aware category filtering on both Product listing and Admin management. When a sport is selected, only categories linked via `SportCategory` (including child sports) are shown
- Pagination on listing pages to keep layouts compact (`page`/`pageSize`, defaults to `24`)
- Login/Register use anti‑forgery tokens and safe `LocalRedirect` validation; non‑local `returnUrl` or unauthorized `/Admin` targets fall back to home
- Product details parse variant attributes into selectable groups; verified purchase is inferred from paid orders
- Favorites and Cart offer variant switching and quick actions (move to cart, save for later)

## Representative Endpoints
- `GET /Product/CategoriesBySport?sportId={id}` — return category items linked to a sport (incl. child sports)
- `POST /Favorites/ToggleFavorite` — add/remove favorite for a variant
- `POST /Favorites/AddToCart` — add variant to cart
- `POST /Favorites/MoveToCart` — move favorite variant to cart
- `POST /Checkout/UpdateQuantity` — change cart quantity
- `POST /Checkout/ChangeVariant` — swap variant in cart line
- `POST /Checkout/SaveAddress` / `POST /Checkout/DeleteAddress` — manage addresses during checkout
- `POST /Checkout/PlaceOrder` — place an order, update stock and sold counts

## Security
- ASP.NET Core Identity for authentication/authorization; admin routes protected by role checks
- Anti‑forgery on form posts; redirect validation to prevent open redirects

## License
This project is intended for academic and educational purposes. Please adapt licensing to your needs.

