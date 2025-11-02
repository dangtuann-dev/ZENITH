using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class AdminLog
    {
        [Key]
        public int LogId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Module { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "text")]
        public string? OldData { get; set; }

        [Column(TypeName = "text")]
        public string? NewData { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
