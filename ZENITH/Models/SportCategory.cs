using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZENITH.Models
{
    // Bảng trung gian cho mối quan hệ N-M giữa Sport và Category
    public class SportCategory
    {
        // Khóa chính kép (Composite Key)
        public int SportId { get; set; }
        public int CategoryId { get; set; }

        // Navigation Properties (Tạo quan hệ)
        public Sport Sport { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}