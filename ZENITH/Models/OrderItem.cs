using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        public int VariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Lưu thông tin variant tại thời điểm đặt hàng
        [StringLength(500)]
        public string? VariantDescription { get; set; } // "Size: L, Color: Red"

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; } = null!;
    }
}
