using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class VariantAttributeValue
    {
        [Key]
        public int Id { get; set; }

        public int VariantId { get; set; }

        public int ValueId { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; } = null!;
        [ForeignKey("ValueId")]
        public virtual AttributeValue AttributeValue { get; set; } = null!;
    }
}
