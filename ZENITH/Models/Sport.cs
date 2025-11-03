using System.ComponentModel.DataAnnotations;

namespace ZENITH.Models
{
    public class Sport
    {
        [Key]
        public int SportId { get; set; }
        [Required]
        [StringLength(100)]
        public string SportName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
        [StringLength(255)]
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt {  get; set; } = DateTime.UtcNow;
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
