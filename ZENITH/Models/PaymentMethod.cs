using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class PaymentMethod
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CardType { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CardHolder { get; set; } = string.Empty;

        [Required]
        [StringLength(7)]
        public string ExpiryDate { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
