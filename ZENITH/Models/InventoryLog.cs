using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class InventoryLog
    {
        [Key]
        public int LogId { get; set; }

        public int VariantId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty;

        public int QuantityChange { get; set; }

        public int QuantityAfter { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; } = null!;
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
