using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace ZENITH.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Avatar { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<AdminLog> AdminLogs { get; set; } = new List<AdminLog>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();
        
    }
}
