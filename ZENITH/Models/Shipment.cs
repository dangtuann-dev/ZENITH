using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    public class Shipment
    {
        [Key]
        public int ShipmentId { get; set; }

        public int OrderId { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [StringLength(100)]
        public string? Carrier { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Preparing";

        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? EstimatedDelivery { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}
