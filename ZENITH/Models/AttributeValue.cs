using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class AttributeValue
    {
        [Key]
        public int ValueId { get; set; }

        public int AttributeId { get; set; }

        [Required]
        [StringLength(100)]
        public string ValueName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ColorCode { get; set; } // Cho thuộc tính màu sắc

        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("AttributeId")]
        public virtual Models.Attribute Attribute { get; set; } = null!;
        public virtual ICollection<VariantAttributeValue> VariantAttributeValues { get; set; } = new List<VariantAttributeValue>();

    }
}
