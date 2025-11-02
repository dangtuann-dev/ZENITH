using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZENITH.Models;

namespace ZENITH.AppData

{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderVoucher> OrderVouchers { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== IDENTITY CONFIGURATION ====================

            // Rename Identity tables (Optional - để tên gọn hơn)
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");

            // ApplicationUser Configuration
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Avatar).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ApplicationRole Configuration
            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            // ==================== ADMIN LOG CONFIGURATION ====================

            modelBuilder.Entity<AdminLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Module).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AdminLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ==================== ADDRESS CONFIGURATION ====================

            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.AddressId);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.AddressLine).IsRequired().HasMaxLength(255);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.District).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Ward).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Addresses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
            });

            // ==================== PAYMENT METHOD CONFIGURATION ====================

            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.CardType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CardHolder).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ExpiryDate).IsRequired().HasMaxLength(7);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.PaymentMethods)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
            });

            // ==================== CATEGORY CONFIGURATION ====================

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(255);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                // Self-referencing relationship for parent-child categories
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(e => e.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CategoryName);
                entity.HasIndex(e => e.ParentCategoryId);
            });

            // ==================== SUPPLIER CONFIGURATION ====================

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.SupplierId);
                entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.SupplierName);
            });

            // ==================== PRODUCT CONFIGURATION ====================

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.StockQuantity).IsRequired();
                entity.Property(e => e.LowStockThreshold).HasDefaultValue(10);
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.Sku).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsFeatured).HasDefaultValue(false);
                entity.Property(e => e.SoldCount).HasDefaultValue(0);
                entity.Property(e => e.ViewCount).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.Products)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Sku).IsUnique();
                entity.HasIndex(e => e.ProductName);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => new { e.IsActive, e.IsFeatured });
            });

            // ==================== PRODUCT IMAGE CONFIGURATION ====================

            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(e => e.ImageId);
                entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IsPrimary).HasDefaultValue(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.ProductImages)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId);
            });

            // ==================== INVENTORY LOG CONFIGURATION ====================

            modelBuilder.Entity<InventoryLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.InventoryLogs)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.InventoryLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ==================== CART ITEM CONFIGURATION ====================

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.CartId);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.AddedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CartItems)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: một user chỉ có một cart item cho một product
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });

            // ==================== FAVORITE CONFIGURATION ====================

            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(e => e.FavoriteId);
                entity.Property(e => e.AddedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Favorites)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: một user chỉ favorite một product một lần
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });

            // ==================== VOUCHER CONFIGURATION ====================

            modelBuilder.Entity<Voucher>(entity =>
            {
                entity.HasKey(e => e.VoucherId);
                entity.Property(e => e.VoucherCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.VoucherType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DiscountValue).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.MaxDiscount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinOrderValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UsedCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.VoucherCode).IsUnique();
                entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate });
            });

            // ==================== ORDER CONFIGURATION ====================

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Discount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(e => e.OrderStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(e => e.Note).HasColumnType("text");
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Address)
                    .WithMany(a => a.Orders)
                    .HasForeignKey(e => e.AddressId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PaymentMethod)
                    .WithMany(pm => pm.Orders)
                    .HasForeignKey(e => e.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderCode).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.OrderDate);
                entity.HasIndex(e => new { e.OrderStatus, e.PaymentStatus });
            });

            // ==================== ORDER ITEM CONFIGURATION ====================

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
            });

            // ==================== ORDER VOUCHER CONFIGURATION ====================

            modelBuilder.Entity<OrderVoucher>(entity =>
            {
                entity.HasKey(e => e.OrderVoucherId);
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderVouchers)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Voucher)
                    .WithMany(v => v.OrderVouchers)
                    .HasForeignKey(e => e.VoucherId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.VoucherId);
            });

            // ==================== ORDER STATUS HISTORY CONFIGURATION ====================

            modelBuilder.Entity<OrderStatusHistory>(entity =>
            {
                entity.HasKey(e => e.HistoryId);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Note).HasColumnType("text");
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderStatusHistories)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.OrderStatusHistories)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ChangedAt);
            });

            // ==================== SHIPMENT CONFIGURATION ====================

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(e => e.ShipmentId);
                entity.Property(e => e.TrackingNumber).HasMaxLength(100);
                entity.Property(e => e.Carrier).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Preparing");

                entity.HasOne(e => e.Order)
                    .WithOne(o => o.Shipment)
                    .HasForeignKey<Shipment>(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TrackingNumber);
                entity.HasIndex(e => e.OrderId).IsUnique();
            });

            // ==================== REVIEW CONFIGURATION ====================

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.ReviewId);
                entity.Property(e => e.Rating).IsRequired();
                entity.Property(e => e.Comment).HasColumnType("text");
                entity.Property(e => e.IsApproved).HasDefaultValue(false);
                entity.Property(e => e.IsVerifiedPurchase).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.IsApproved, e.CreatedAt });
            });

            // ==================== SEED DATA (OPTIONAL) ====================

            //SeedData(modelBuilder);
        }

        //private void SeedData(ModelBuilder modelBuilder)
        //{
        //    // Seed Roles
        //    modelBuilder.Entity<ApplicationRole>().HasData(
        //        new ApplicationRole
        //        {
        //            Id = "1",
        //            Name = "Admin",
        //            NormalizedName = "ADMIN",
        //            Description = "Full system access",
        //            ConcurrencyStamp = Guid.NewGuid().ToString()
        //        },
        //        new ApplicationRole
        //        {
        //            Id = "2",
        //            Name = "Manager",
        //            NormalizedName = "MANAGER",
        //            Description = "Store and inventory management",
        //            ConcurrencyStamp = Guid.NewGuid().ToString()
        //        },
        //        new ApplicationRole
        //        {
        //            Id = "3",
        //            Name = "Staff",
        //            NormalizedName = "STAFF",
        //            Description = "Order processing and customer support",
        //            ConcurrencyStamp = Guid.NewGuid().ToString()
        //        },
        //        new ApplicationRole
        //        {
        //            Id = "4",
        //            Name = "Customer",
        //            NormalizedName = "CUSTOMER",
        //            Description = "Regular customer",
        //            ConcurrencyStamp = Guid.NewGuid().ToString()
        //        }
        //    );

        //    // Seed Categories
        //    modelBuilder.Entity<Category>().HasData(
        //        new Category { CategoryId = 1, CategoryName = "Vegetables", DisplayOrder = 1 },
        //        new Category { CategoryId = 2, CategoryName = "Fruits", DisplayOrder = 2 },
        //        new Category { CategoryId = 3, CategoryName = "Dairy & Eggs", DisplayOrder = 3 },
        //        new Category { CategoryId = 4, CategoryName = "Meat & Seafood", DisplayOrder = 4 },
        //        new Category { CategoryId = 5, CategoryName = "Beverages", DisplayOrder = 5 }
        //    );
        //}
    }
}
