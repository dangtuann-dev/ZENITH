using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class CartItem
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int VariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; } = null!;
    }
}
