using System.ComponentModel.DataAnnotations;

namespace ZENITH.Models
{
    public class Attribute
    {
        [Key]
        public int AttributeId { get; set; }

        [Required]
        [StringLength(50)]
        public string AttributeName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(50)]
        public string InputType { get; set; } = "select"; // select, color, text

        public bool IsRequired { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public virtual ICollection<AttributeValue> AttributeValues { get; set; } = new List<AttributeValue>();

    }
}
