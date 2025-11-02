using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class OrderVoucher
    {
        [Key]
        public int OrderVoucherId { get; set; }

        public int OrderId { get; set; }

        public int VoucherId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
        [ForeignKey("VoucherId")]
        public virtual Voucher Voucher { get; set; } = null!;
    }
}
