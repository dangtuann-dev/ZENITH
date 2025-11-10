using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ZENITH.AppData;
using ZENITH.Models;

public static class DbInitializer
{
    private static int sportDisplayOrder = 1;
    private static int categoryDisplayOrder = 1;
    private static int productIndex = 1; // Biến đếm Product Index
    private static int variantIndex = 1; // Biến đếm Variant Index

    // MẢNG DỮ LIỆU SẢN PHẨM: productData (Đã xác nhận là đúng và hoàn chỉnh)
    public static readonly dynamic[] productData = new[]
  {
    //sport Hiking & Trekking
    //category Balo & Túi
    //HT001
    new {
        Name = "Balo du lịch nhỏ gọn 10 L - NH Arpenaz 50 xanh navy",
        Desc = "Mục tiêu của chúng tôi là mang đến cho bạn chiếc balo 10 L với mức giá hợp lý, giúp bạn bảo quản những vật dụng thiết yếu an toàn trên mọi cung đường núi có độ dốc vừa phải.",
        CategoryName = "Balo & Túi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-001",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-001-10L", Stock = 150, Price = 79000, SalePrice = 69000, Attributes = "10L" },
        },
        ImageCount = 4
    },
    //HT002
    new {
        Name = "Balo leo núi du lịch 30L - Arpenaz NH100 đen",
        Desc = "Mẫu NH Arpenaz100 30 L tiện nghi và đầy đủ phụ kiện của chúng tôi là người bạn đồng hành lý tưởng cho những chuyến đi bộ với địa hình hơi gồ ghề của bạn.",
        CategoryName = "Balo & Túi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-002",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-002-30L", Stock = 150, Price = 199000, SalePrice = 199000, Attributes = "30L" },
        },
        ImageCount = 4
    },
    //HT003
    new {
        Name = "Balo dã ngoại 32L - NH Escape 500 đen",
        Desc = "Balo mang lại sự thoải mái nhờ đệm lót êm ái, tiện dụng với 3 ngăn lớn và 15 túi nhỏ, thông minh với kích thước hành lý xách tay và các tính năng tiện lợi.",
        CategoryName = "Balo & Túi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-003",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-003-32L", Stock = 150, Price = 999000, SalePrice = 999000, Attributes = "32L" },
        },
        ImageCount = 4
    },
    //HT004
    new {
        Name = "Balo leo núi 38L - MH500 nâu",
        Desc = "Balo nhẹ với phần khung có thể điều chỉnh. Phần lưng được thiết kế thoáng khí tối ưu, giúp mang lại cảm giác thoải mái vượt trội trong các chuyến hiking.",
        CategoryName = "Balo & Túi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-004",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-004-38L", Stock = 150, Price = 2399000, SalePrice = 2399000, Attributes = "38L" },
        },
        ImageCount = 4
    },
    //Quần Áo Leo Núi
    //HT005
    new {
        Name = "Quần dài trekking tháo ống bền bỉ - MT100 xám",
        Desc = "Quần dài trekking có thể tháo ống nhanh chóng để chuyển thành quần short để phù hợp với các điều kiện thời tiết khác nhau.",
        CategoryName = "Quần Áo Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-005",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-005-S", Stock = 150, Price = 539000, SalePrice = 499000, Attributes = "S / W30 L33 " },
            new { Sku = "HT-005-M", Stock = 150, Price = 539000, SalePrice = 499000, Attributes = "M / W32 L33 " },
            new { Sku = "HT-005-ML", Stock = 150, Price = 539000, SalePrice = 499000, Attributes = "M/L / W33 L33 " },
            new { Sku = "HT-005-L", Stock = 150, Price = 539000, SalePrice = 499000, Attributes = "L / W34 L34 " },
            new { Sku = "HT-005-XL", Stock = 150, Price = 539000, SalePrice = 499000, Attributes = "XL / W37 L34 " }
        },
        ImageCount = 4
    },
    //HT006
    new {
        Name = "Áo khoác chống nắng nam - Helium MH500 xanh dương/trắng",
        Desc = "Áo khoác gió với chỉ số UPF 50+, nhẹ, từng đạt giải. Trợ thủ chống nắng gió cho những chuyến đi ngoài trời.Được thiết kế cho những chuyến đi dưới trời nắng. Bảo vệ bạn trước tia UVA/UAB từ ánh nắng mặt trời, kèm theo khả năng cản gió, nhẹ, vải co giãn và thoáng khí.",
        CategoryName = "Quần Áo Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-006",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-006-S", Stock = 150, Price = 1199000, SalePrice = 899000, Attributes = "S" },
            new { Sku = "HT-006-M", Stock = 150, Price = 1199000, SalePrice = 899000, Attributes = "M" },
            new { Sku = "HT-006-ML", Stock = 150, Price = 1199000, SalePrice = 899000, Attributes = "ML" },
            new { Sku = "HT-006-L", Stock = 150, Price = 1199000, SalePrice = 899000, Attributes = "L" },
            new { Sku = "HT-006-XL", Stock = 150, Price = 1199000, SalePrice = 899000, Attributes = "XL" }
        },
        ImageCount = 4
    },
    //HT007
    new {
        Name = "Áo khoác chống nắng - 900 đen",
        Desc = "Áo khoác gió hiking nhẹ, được làm từ sợi chống UV và đạt giải thưởng. Trợ thủ chống nắng gió cho những chuyến đi ngoài trời.Được thiết kế cho những chuyến đi dưới trời nắng. Bảo vệ bạn trước tia UVA/UAB từ ánh nắng mặt trời, kèm theo khả năng cản gió, nhẹ và thoáng khí.",
        CategoryName = "Quần Áo Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-007",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-007-S", Stock = 150, Price = 1399000, SalePrice = 1299000, Attributes = "S" },
            new { Sku = "HT-007-M", Stock = 150, Price = 1399000, SalePrice = 1299000, Attributes = "M" },
            new { Sku = "HT-007-ML", Stock = 150, Price = 1399000, SalePrice = 1299000, Attributes = "ML" },
            new { Sku = "HT-007-L", Stock = 150, Price = 1399000, SalePrice = 1299000, Attributes = "L" },
            new { Sku = "HT-007-XL", Stock = 150, Price = 1399000, SalePrice = 1299000, Attributes = "XL" },
        },
        ImageCount = 4
    },
    //Giày Leo Núi
    //HT008
    new {
        Name = "Giày leo núi hiking cổ lửng - NH100 đen",
        Desc = "Giày nhẹ, được thiết kế với dây buộc chính xác, mang lại cảm giác thoải mái và nâng đỡ chân hiệu quả trên mọi địa hình gồ ghề. Mẫu giày thân thiện với môi trường và đồng hành cùng bạn trong những chuyến hiking vùng thấp, trong rừng hoặc trên bờ biển giữa tiết trời khô ráo.",
        CategoryName = "Giày Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-008",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-008-40", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "40" },
            new { Sku = "HT-008-41", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "41" },
            new { Sku = "HT-008-42", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "42" },
            new { Sku = "HT-008-43", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "43" },
            new { Sku = "HT-008-44", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "44" },
            new { Sku = "HT-008-45", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "45" },
            new { Sku = "HT-008-46", Stock = 150, Price = 519000, SalePrice = 519000, Attributes = "46" }
        },
        ImageCount = 4
    },
    //HT009
    new {
        Name = "Giày leo núi nữ chống thấm cổ lửng - MH100 xám/xanh dương",
        Desc = "Mang lại cho đôi chân sự thoải mái và bảo vệ nhờ khả năng giảm chấn dọc đế giày, nâng đỡ từ thân giày cao và lớp màng chống thấm tuyệt đối để giữ chân luôn khô ráo. Giày thể thao chống thấm dành cho những chuyến đi bộ leo núi không thường xuyên, được thiết kế tại chân núi Mont Blanc!\r\n",
        CategoryName = "Giày Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-009",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-009-40", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "40" },
            new { Sku = "HT-009-41", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "41" },
            new { Sku = "HT-009-42", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "42" },
            new { Sku = "HT-009-43", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "43" },
            new { Sku = "HT-009-44", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "44" },
            new { Sku = "HT-009-45", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "45" },
            new { Sku = "HT-009-46", Stock = 150, Price = 1099000, SalePrice = 1099000, Attributes = "46" },
        },
        ImageCount = 4
    },
    //HT010
    new {
        Name = "Giày dã ngoại cổ lửng chống thấm - SH 500 trắng/xám",
        Desc = "Bạn đang tìm giày hiking hay giày thể thao thoải mái cho chuyến dã ngoại mùa đông? Giày 2 trong 1. Kiểu dáng hiện đại của giày giúp bạn dễ dàng kết hợp giữa đi hiking và các hoạt động hàng ngày. Giày dã ngoại giữ ấm và chống thấm cho những chuyến đi vào mùa đông. Giày kết hợp giữa sự thoải mái, độ bám và phong cách.",
        CategoryName = "Giày Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-010",
        IsFeat = true,
        // Dữ liệu Variants
        Variants = new[] {
            new { Sku = "HT-010-40", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "40" },
            new { Sku = "HT-010-41", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "41" },
            new { Sku = "HT-010-42", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "42" },
            new { Sku = "HT-010-43", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "43" },
            new { Sku = "HT-010-44", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "44" },
            new { Sku = "HT-010-45", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "45" },
            new { Sku = "HT-010-46", Stock = 150, Price = 1399000, SalePrice = 1399000, Attributes = "46" },
        },
        ImageCount = 4
    },
    //Phụ Kiện Leo Núi
    //HT011
    new {
        Name = "Mũ lưỡi trai trekking - Travel 100 xanh đen",
        Desc = "Nếu bạn đang tìm kiếm một chiếc mũ đơn giản với mức giá phải chăng để bảo vệ bạn khỏi ánh nắng mặt trời, chiếc mũ này chính là dành cho bạn! Mũ lưỡi trai trekking đảm bảo tất cả tính năng cần thiết khi cho chuyến đi ngoài trời với giá tốt!",
        CategoryName = "Phụ Kiện Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-011",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-011", Stock = 150, Price = 53000, SalePrice = 53000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
    //HT012
    new {
        Name = "Khăn đa năng TREK 100 - Xám",
        Desc = "Khăn chui đầu này bảo vệ đầu khỏi nắng, che cổ khi trời lạnh hoặc thấm nước mưa trên trán. Không thể thiếu trong balô của bạn. Chúng tôi thiết kế mẫu khăn đa năng này để đồng hành cùng bạn trong mọi chuyến đi!",
        CategoryName = "Phụ Kiện Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-012",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-012", Stock = 150, Price = 79000, SalePrice = 79000, Attributes = "Một Cỡ Duy Nhất" },

        },
        ImageCount = 4
    },
    //HT013
    new {
        Name = "Kính mát thể thao tròng kính phân cực - MH 530 đen",
        Desc = "Tròng kính chống tia cực tím loại 3 ngăn 100% các tia độc hại từ mặt trời giúp bạn không bị chói mắt. Nhờ trọng lượng nhẹ và bám tốt, kính mát bảo vệ bạn tối đa khi đi hiking. Kính mát chống tia UV, đồng hành cùng bạn trong các chuyến hiking. Sản phẩm lý tưởng dành cho người leo núi thường xuyên nhờ trọng lượng nhẹ.",
        CategoryName = "Phụ Kiện Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-013",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-013", Stock = 150, Price = 399000, SalePrice = 369000, Attributes = "Một Cỡ Duy Nhất" },

        },
        ImageCount = 4
    },
    //HT014
    new {
        Name = "Bộ sơ cứu 47 món - 500 UL",
        Desc = "Các vật dụng trong bộ sơ cứu giúp xử lý các vết thương nhẹ hoặc bong gân. Bộ sơ cứu có đầy đủ các vật dụng đa dạng để sử dụng khi cần.  Bộ sơ cứu được thiết kế giúp xử lý vết thương, vết cắt và thực hiện những biện pháp sơ cứu cơ bản trong các chuyến trekking.",
        CategoryName = "Phụ Kiện Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-014",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-014", Stock = 150, Price = 499000, SalePrice = 399000, Attributes = "Một Cỡ Duy Nhất" },

        },
        ImageCount = 4
    },
    //Vớ Leo Núi
    //HT015
    new {
        Name = "Set 2 đôi tất cổ cao - Hike 50 xám",
        Desc = "Tất mang lại sự thoải mái tối đa khi đi dã ngoại, dù mang với giày cổ thấp, vừa hay cao. Bán theo bộ 2 đôi cùng màu. Sản phẩm được thiết kế để sử dụng hàng ngày, cũng như đồng hành cùng bạn trong các chuyến hiking.",
        CategoryName = "Vớ Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-015",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-015-35-38", Stock = 150, Price = 129000, SalePrice = 129000, Attributes = "35/38" },
            new { Sku = "HT-015-39-42", Stock = 150, Price = 129000, SalePrice = 129000, Attributes = "39/42" },
            new { Sku = "HT-015-42-46", Stock = 150, Price = 129000, SalePrice = 129000, Attributes = "43/46" },

        },
        ImageCount = 4
    },
    //HT016
    new {
        Name = "Tất dã ngoại cổ vừa ấm áp - SH500 đen",
        Desc = "Mặt trong tất được dệt bằng len Merino cho khả năng giữ nhiệt tối đa, từ đó mang lại cảm giác thoải mái và ấm áp vượt trội. Mặt ngoài tất được làm từ sợi acrylic giúp tăng độ bền. Sản phẩm thiết thực, giúp mang lại cảm giác ấm áp, đồng hành cùng bạn trong những chuyến dã ngoại giữa thời tiết lạnh giá.",
        CategoryName = "Vớ Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-016",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-016-35-38", Stock = 150, Price = 399000, SalePrice = 399000, Attributes = "35/38" },
            new { Sku = "HT-016-39-42", Stock = 150, Price = 399000, SalePrice = 339000, Attributes = "39/42" },
            new { Sku = "HT-016-43-46", Stock = 150, Price = 399000, SalePrice = 339000, Attributes = "43/46" }
        },
        ImageCount = 4
    },
    //HT017
    new {
        Name = "Xà cạp trekking cổ cao - MT 500 đen",
        Desc = "Xà cạp cổ cao siêu bền, được thiết kế với dáng suông ôm chân vừa vặn, là sự lựa chọn hoàn hảo cho các chuyến hiking hoặc đi bộ trên mặt đất ẩm ướt. Xà cạp được thiết kế giúp ngăn đất đá chui vào giày, đồng hành cùng bạn trong các chuyến trekking.",
        CategoryName = "Vớ Leo Núi",
        SportName = "Hiking & Trekking",
        SkuBase = "HT-017",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-017-SM", Stock = 150, Price = 699000, SalePrice = 699000, Attributes = "S/M" },
            new { Sku = "HT-017-LXL", Stock = 150, Price = 699000, SalePrice = 699000, Attributes = "L/XL" }
        },
        ImageCount = 4
    },
    //Cắm Trại
    //Lều Cắm Trại
    //HT018
    new {
        Name = "Lều cắm trại 2 người - MH100 xám",
        Desc = "Giá cả phải chăng. Với trọng lượng 4,1kg, kích thước 197cm x 210cm và chiều cao 116cm. Sự lựa chọn hoàn hảo cho lần đầu cắm trại.Lều cắm trại cho 2 người, đơn giản và dễ dựng.",
        CategoryName = "Lều Cắm Trại",
        SportName = "Cắm Trại",
        SkuBase = "HT-018",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-018", Stock = 150, Price = 769000, SalePrice = 699000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
     //HT019
    new {
        Name = "Lều cắm trại 3 người - MH100 Fresh&Black trắng",
        Desc = "Công nghệ Fresh & Black cho giấc ngủ ngon nhờ khả năng thông gió và ngăn ánh sáng. Lều cắm trại Fresh & Black MH100 cho 3 người phù hợp mọi thời tiết.",
        CategoryName = "Lều Cắm Trại",
        SportName = "Cắm Trại",
        SkuBase = "HT-019",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-019", Stock = 150, Price = 1499000, SalePrice = 1499000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
     //HT020
    new {
        Name = "Lều dã ngoại bơm hơi Fresh & Black 4 người - Air Seconds 4.1 xanh dương/trắng",
        Desc = "Động lực của chúng tôi? Mang đến cho bạn một chiếc lều tự bơm hơi dựng nhanh công nghệ Fresh&Black của chúng tôi. Giảm nhiệt và ánh sáng trong lều, do vậy bạn có thể thức dậy bất kỳ khi nào bạn muốn! Lều dã ngoại bơm hơi dành cho 4 người, mang đến sự thoải mái tối đa với không gian ngủ và sinh hoạt rộng rãi.",
        CategoryName = "Lều Cắm Trại",
        SportName = "Cắm Trại",
        SkuBase = "HT-020",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-020", Stock = 150, Price = 9999000, SalePrice = 9999000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
    //Túi Ngủ & Đệm Hơi
     //HT021
    new {
        Name = "Túi ngủ cắm trại Arpenaz 20° - Xanh lá",
        Desc = "20°C. Mục tiêu của chúng tôi? Mang đến cho bạn một chiếc túi ngủ thân thiện với môi trường với khóa kéo gắn sẵn, dễ dàng đóng mở. Có thể sử dụng như vỏ chăn. Bảo hành 5 năm! Túi ngủ Arpenaz 20° được thiết kế thân thiện với môi trường, mang đến cho bạn những giấc ngủ thoải mái ở nhiệt độ gần 20°C trong các chuyến cắm trại.",
        CategoryName = "Túi Ngủ & Đệm Hơi",
        SportName = "Cắm Trại",
        SkuBase = "HT-021",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-021", Stock = 150, Price = 499000, SalePrice = 399000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
     //HT022
    new {
        Name = "Túi ngủ trekking 5°C vải tổng hợp - MT500 đen",
        Desc = "Túi ngủ có đệm này được thiết kế lại để nhẹ hơn và nhỏ gọn hơn, do đó phù hợp hơn với nhu cầu của bạn. Để dễ sử dụng hơn, túi mở ra dọc theo 3/4 chiều dài.  Với hình dạng \"xác ướp\" và mũ trùm đầu, mẫu túi ngủ trekking này sẽ giúp bạn ngủ ngon vào ban đêm ở nhiệt độ từ 5°C trở lên.",
        CategoryName = "Túi Ngủ & Đệm Hơi",
        SportName = "Cắm Trại",
        SkuBase = "HT-022",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-022", Stock = 150, Price = 2399000, SalePrice = 2399000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
     //HT023
    new {
        Name = "Túi ngủ cắm trại đa dụng Arpenaz 15°",
        Desc = "Nhiệt độ thoải mái 15°C. Động lực của chúng tôi? Là mang đến cho bạn chiếc túi ngủ thân thiện môi trường, có khóa kéo dọc túi. Túi có thể biến thành chăn bông và có thể ghép 2 túi. Bảo hành 5 năm Túi ngủ Arpenaz 15° thân thiện với môi trường để bạn có thể ngủ thoải mái khi cắm trại dưới nhiệt độ xấp xỉ 15°C.",
        CategoryName = "Túi Ngủ & Đệm Hơi",
        SportName = "Cắm Trại",
        SkuBase = "HT-023",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-023", Stock = 150, Price = 639000, SalePrice = 639000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
    //Ghế Cắm Trại
     //HT024
    new {
        Name = "Ghế cắm trại gấp gọn chân thấp - 100 nâu",
        Desc = "Ghế có trọng lượng chỉ 1,2 kg siêu nhỏ gọn, dễ dàng vận chuyển với dây đeo tích hợp. Siêu nhỏ gọn, bạn có thể quên là đang mang theo ghế. Ghế cắm trại được thiết kế với chiều cao lý tưởng 25 cm, mang lại cảm giác thoải mái và chắc chắn khi ngồi. Mang lại những phút giây thư giãn ngoài trời.",
        CategoryName = "Ghế Cắm Trại",
        SportName = "Cắm Trại",
        SkuBase = "HT-024",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-024", Stock = 150, Price = 459000, SalePrice = 399000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
    //HT025
    new {
        Name = "Ghế tựa cắm trại 5 tư thế thoải mái và nhỏ gọn - Màu be",
        Desc = "Ghế nhỏ gọn và tiện lợi, có thể gấp gọn để mang theo bất cứ đâu. Có thể xách tay với tay cầm hoặc đeo trên vai với dây đeo. Ghế tựa cắm trại kèm túi đựng, được thiết kế với 2 tay vịn và 5 tư thế, đầu tựa có thể điều chỉnh. Tiện lợi, mang lại những phút giây thư giãn ngoài trời!",
        CategoryName = "Ghế Cắm Trại",
        SportName = "Cắm Trại",
        SkuBase = "HT-025",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-025", Stock = 150, Price = 2199000, SalePrice = 2199000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    },
    //HT026
    new {
        Name = "Ghế cắm trại gấp gọn dáng thấp - 500 vàng/xám/đen",
        Desc = "Ghế xếp gọn tiết kiệm không gian, là sự lựa chọn hoàn hảo cho buổi cắm trại. Ghế cắm trại dã ngoại gấp gọn MH500 siêu gọn nhẹ cho chuyến đi cuối tuần.",
        CategoryName = "Ghế Cắm Trại",
        SportName = "Cắm Trại",
        SkuBase = "HT-026",
        IsFeat = true,
        Variants = new[] {
            new { Sku = "HT-026", Stock = 150, Price = 1499000, SalePrice = 1299000, Attributes = "Một Cỡ Duy Nhất" },
        },
        ImageCount = 4
    }
    //HT027
   



};



    // Dictionary cho Sport Icon (Sử dụng ký hiệu ~/ dẫn đến wwwroot)
    private static Dictionary<string, string> SportIcons = new Dictionary<string, string>()
    {
        { "Leo Núi & Dã Ngoại", "~/image/categoryImage/sport/hiking-trekking.svg" },
        { "Chạy Bộ & Đi Bộ", "~/image/categoryImage/sport/chay-bo-duong-truong.svg" },
        { "Yoga & Pilates", "~/image/categoryImage/sport/quan-ao-yoga.svg" },
        { "Bơi Lội", "~/image/categoryImage/sport/do-boi-loi.svg" },
        { "Võ Thuật Tổng Hợp", "~/image/categoryImage/sport/boxing.svg" },
        { "Đạp Xe", "~/image/categoryImage/sport/xe-dap.svg" },
        { "Thể Thao Dùng Vợt", "~/image/categoryImage/sport/cau-long.svg" },
        { "Thể Thao Đồng Đội", "~/image/categoryImage/sport/football.svg" },

        // Sport Con Icons 
        { "Hiking & Trekking", "~/image/categoryImage/sport/hiking-trekking.svg" },
        { "Cắm Trại", "~/image/categoryImage/sport/camtrai.svg" },
        { "Leo Núi Nhân Tạo", "~/image/categoryImage/sport/leo-nui-nhan-tao.svg" },
        { "Chạy Bộ Đường Trường", "~/image/categoryImage/sport/chay-bo-duong-truong.svg" },
        { "Chạy Địa Hình (Trail)", "~/image/categoryImage/sport/trail.svg" },
        { "Đi Bộ", "~/image/categoryImage/sport/di-bo.svg" },
        { "Quần Áo Yoga & Pilates", "~/image/categoryImage/sport/quan-ao-yoga.svg" },
        { "Thảm yoga pilates & túi đựng thảm", "~/image/categoryImage/sport/tham-tui-yoga.svg" },
        { "Đồ bơi", "~/image/categoryImage/sport/do-boi-loi.svg" },
        { "Kính bơi", "~/image/categoryImage/sport/kinh-mu-boi-loi.svg" },
        { "Mũ Bơi", "~/image/categoryImage/sport/kinh-mu-boi-loi.svg" },
        { "Phụ Kiện bơi", "~/image/categoryImage/sport/phu-kien-boi.svg" },
        { "Xe đạp", "~/image/categoryImage/sport/xe-dap.svg" },
        { "Phụ kiện xe đạp", "~/image/categoryImage/sport/phu-kien-xe-dap.svg" },
        { "Quần Áo Đạp Xe", "~/image/categoryImage/sport/quan-ao-xe-dap.svg" },
        { "Phụ Tùng & Bảo Dưỡng", "~/image/categoryImage/sport/phu-tung-bao-duong-xe-dap.svg" },
        { "Boxing & Muay Thai", "~/image/categoryImage/sport/boxing.svg" },
        { "Cầu Lông", "~/image/categoryImage/sport/cau-long.svg" },
        { "TENNIS", "~/image/categoryImage/sport/tennis.svg" },
        { "Bóng Bàn", "~/image/categoryImage/sport/bong-ban.svg" },
        { "Pickleball", "~/image/categoryImage/sport/pickleball.svg" },
        { "Bóng Đá & Futsal", "~/image/categoryImage/sport/football.svg" },
        { "Bóng rổ", "~/image/categoryImage/sport/bong-ro.svg" },
        { "Bóng chuyền", "~/image/categoryImage/sport/bong-chuyen.svg" },
        { "Bóng chày", "~/image/categoryImage/sport/bong-chay.svg" },
    };

    // Mô tả chi tiết cho Categories
    private static Dictionary<string, string> CategoryDescriptions = new Dictionary<string, string>()
    {
        { "Balo & Túi", "Mua balo leo núi, balo du lịch siêu gọn nhẹ ngay tại Zenith với giá chỉ từ 69K. Zenith luôn nỗ lực từng ngày để mang đến sản phẩm tốt nhất dành cho khách hàng yêu thể thao. Tại Zenith bạn sẽ tìm thấy balo & túi mọi kích cỡ: từ balo 10L gọn nhẹ cho đến 80L cho chuyến đi dài ngày.\r\n" },
        { "Quần Áo Leo Núi", "Một bộ quần áo leo núi tốt, thoải mái và bền bỉ sẽ là người bạn đồng hành vô cùng quan trọng trong chuyến leo núi của bạn. Hãy cùng Zenith khám phá ngay bộ sưu tập quần áo leo núi mới nhất, phù hợp với phong cách và nhu cầu của bạn!\r\n" },
        { "Giày Leo Núi", "Zenith tự hào là cửa hàng thể thao lớn chuyên về giày leo núi trekking với đa dạng mẫu mã: từ giày leo núi chống thấm nước, giày trekking lội suối. Tại Zenith, bạn có thể tìm thấy nhiều từ giày leo núi cổ cao, cổ vừa cho đến cổ thấp để phù hợp với nhu cầu của bạn. Giày leo núi ở Zenith được bảo hành tối thiểu 2 năm, với giá cực kì tốt chỉ từ 275K.\r\n" },
        { "Lều Cắm Trại", "Trong bối cảnh con người ngày càng tìm về thiên nhiên, những buổi cắm trại đã trở thành một xu hướng được yêu thích. Để đáp ứng nhu cầu này, Zenith giới thiệu các dòng lều cắm trại đa dạng, từ lều gia đình đến lều cá nhân, với chất lượng tốt nhất, mang đến sự thoải mái và an toàn cho mọi chuyến đi.\r\n" },
        { "Túi Ngủ & Đệm Hơi", "Với hàng loạt lợi ích tuyệt vời, nệm hơi đã và đang trở thành lựa chọn hàng đầu của nhiều gia đình trong những chuyến leo núi, cắm trại. Thấu hiểu tâm lý đó, Zenith mang đến cho bạn đa dạng lựa chọn với hơn 50++ mẫu nệm hơi cắm trại chất lượng giá rẻ chỉ từ 145K. Chi tiết tìm hiểu ngay sau đây!\r\n" },
        { "Ghế Cắm Trại", "Những chuyến đi cắm trại sẽ trở nên thú vị hơn bao giờ hết khi bạn có một chiếc ghế cắm trại ưng ý. Với đa dạng mẫu mã, kiểu dáng và chất liệu, danh mục ghế cắm trại của Zenith sẽ mang đến cho bạn những lựa chọn tuyệt vời. Từ những chiếc ghế xếp gọn nhẹ, dễ mang theo đến những chiếc ghế bành rộng rãi, thoải mái, tất cả đều có sẵn tại cửa hàng của chúng tôi.\r\n" },
        { "Giày Chạy Bộ", "Zenith tự hào là thương hiệu thể thao chính hãng đến từ Pháp, cung cấp đa dạng các mẫu sản phẩm giày chạy bộ chất lượng từ nhiều thương hiệu khác nhau như: KALENJI, KIPRUN,,... Tại đây, khách hàng có thể dễ dàng tìm kiếm hơn 100+ mẫu giày chạy bộ với mức giá vô cùng hấp dẫn chỉ từ 295K. \r\n" },
        { "Giày Chạy Trail", "Giày chạy trail là người bạn đồng hành không thể thiếu của những vận động viên yêu thích chinh phục địa hình gồ ghề. Với nhiều năm kinh nghiệm trong lĩnh vực thể thao, Zenith đã nghiên cứu và phát triển những mẫu giày chạy trail đa dạng, phù hợp với mọi đối tượng và phong cách chạy!\r\n" },
        { "Giày Đi Bộ", "Đi bộ không chỉ là một hình thức tập thể dục, mà còn là cách tuyệt vời để thư giãn và khám phá thế giới xung quanh. Để có những trải nghiệm đi bộ thật sự thoải mái và hiệu quả, việc chọn một đôi giày phù hợp là vô cùng quan trọng. Zenith tự hào mang đến cho bạn những đôi giày đi bộ chất lượng cao, đáp ứng mọi nhu cầu của bạn.\r\n" },
        { "Áo Tập Yoga", "Khám phá danh mục áo tập yoga tại Zenith với hơn 20 mẫu sản phẩm đa dạng, từ các thiết kế cơ bản đến nâng cao, đảm bảo mang đến sự thoải mái và linh hoạt cho mọi buổi tập. Chất liệu mềm mại như lụa và cotton kết hợp với công nghệ co dãn hiện đại giúp áo vừa vặn và thoải mái trong mọi động tác yoga.\r\n" },
        { "Áo Ngực Tập Yoga", "Bra, hay còn gọi là áo ngực lót, là loại trang phục không thể thiếu đối với nữ giới trong quá trình tập luyện thể dục, rèn luyện cơ thể, đặc biệt là bộ môn Yoga với nhiều công dụng hữu ích như góp phần nâng đỡ, bảo vệ vòng một. Sau đây là gợi ý 10+ áo bra tập Yoga chính hãng.\r\n" },
        { "Tất Chống Trượt", "Trong các môn thể thao vận động, tất chống trượt được coi là sản phẩm hữu dụng giúp bạn có thể tự tin trong các hoạt động thể chất. Hiện nay, Zenith đang cung cấp đến khách hàng hơn 20++ mẫu tất chống trượt cho cả trẻ nhỏ và người lớn với mức giá chỉ từ 125K. Tất cả các sản phẩm tại Zenith đều đạt chuẩn chất lượng châu u, đáp ứng những tiêu chuẩn khắt khe nhất về độ bền và hiệu quả sử dụng.\r\n" },
        { "Đồ tập yoga nam", "Bạn đang tìm kiếm đồ tập yoga cho nam chất lượng cao, thoải mái và phong cách? Zenith chính là địa chỉ uy tín để bạn lựa chọn những trang phục yoga phù hợp nhất! Chúng tôi luôn cung cấp đa dạng các mẫu mã đồ tập yoga cho nam với đầy đủ các kiểu dáng, màu sắc và chất liệu khác nhau.\r\n" },
        { "Đồ tập yoga nữ", "Yoga - bộ môn thể thao giúp rèn luyện sức khỏe và tinh thần đang ngày càng được ưa chuộng bởi phái nữ. Để có những trải nghiệm tuyệt vời nhất trong mỗi buổi tập, việc lựa chọn trang phục phù hợp là vô cùng quan trọng. Hiểu được nhu cầu đó, Zenith mang đến cho bạn tủ đồ tập yoga nữ đẹp, đa dạng về mẫu mã, chất lượng cao và giá cả hợp lý.\r\n" },
        { "Thảm yoga & Pilates", "Thảm yoga là vật dụng cần thiết không chỉ hỗ trợ người dùng nâng cao hiệu quả tập luyện mà còn hỗ trợ phòng tránh các chấn thương. Hãy cùng Zenith khám phá các mẫu thảm yoga chất lượng, bán chạy nhất hiện nay ngay sau đây nhé!\r\n" },
        { "Túi đựng thảm yoga", "Với ưu điểm nổi trội về chất liệu, thiết kế cũng như màu sắc, túi đựng thảm Yoga chính hãng Zenith tự tin trở thành bạn đồng hành lý tưởng cho những tín đồ Yoga. Thông tin chi tiết về các sản phẩm, vui lòng xem ngay tại bài viết bên dưới.\r\n" },
        { "Xe Đạp Đường Trường", "Xe đạp đường trường (road bike) là lựa chọn lý tưởng cho những ai yêu thích tốc độ, sự bền bỉ và những hành trình dài trên đường nhựa. Tại Zenith, bạn có thể dễ dàng tìm thấy các mẫu xe đạp đường trường chính hãng, chất lượng châu Âu với mức giá hợp lý, phù hợp cho cả người mới bắt đầu lẫn những tay đua nhiều kinh nghiệm.\r\n" },
        { "Xe Đạp Địa Hình & Hybrid", "Xe đạp địa hình (MTB) là một lựa chọn tuyệt vời cho những ai yêu thích khám phá và chinh phục các cung đường hiểm trở. Với sự đa dạng về kiểu dáng và tính năng, việc chọn được chiếc xe đạp phù hợp với nhu cầu sử dụng và thể trạng là vô cùng quan trọng.\r\n" },
        { "Xe Đạp Gấp", "Xe đạp gấp gọn người lớn đang trở thành lựa chọn phổ biến nhờ thiết kế tiện lợi, trọng lượng nhẹ và khả năng di chuyển linh hoạt trong đô thị. Tại Zenith, bạn dễ dàng tìm thấy nhiều mẫu xe đạp gấp với chất lượng cao, giá cả hợp lý, phù hợp cho cả việc đi làm, đi học hay vui chơi cuối tuần.\r\n" },
        { "Xe Đạp Thành Phố", "Xe đạp gấp gọn người lớn đang trở thành lựa chọn phổ biến nhờ thiết kế tiện lợi, trọng lượng nhẹ và khả năng di chuyển linh hoạt trong đô thị. Tại Zenith, bạn dễ dàng tìm thấy nhiều mẫu xe đạp gấp với chất lượng cao, giá cả hợp lý, phù hợp cho cả việc đi làm, đi học hay vui chơi cuối tuần.\r\n" },
        { "Đèn Xe Đạp", "Lựa chọn đèn xe đạp phù hợp không chỉ đảm bảo an toàn mà còn giúp tiết kiệm chi phí lâu dài, tăng tính an toàn khi luyện tập. Tham khảo 14+ sản phẩm đèn xe đạp, đèn LED gắn trước và sau xe chỉ từ 129.000 VND\r\n" },
        { "Khóa xe đạp", "Không chỉ giúp tránh mất cắp, khóa xe đạp còn là một phụ kiện không thể thiếu khi đi xe đạp ngoài trời, nhất là ở khu vực đông người. Danh sách các mẫu khóa xe đạp được ưa chuộng tại Zenith, với thiết kế tiện dụng, độ bảo mật cao và phù hợp với nhiều nhu cầu sử dụng khác nhau.\r\n" },
        { "Túi xe đạp", "Dù bạn là dân phượt dày dặn kinh nghiệm hay chỉ đơn giản muốn tận hưởng những buổi đạp xe thư giãn cuối tuần, thì một chiếc túi treo xe đạp chất lượng luôn là phụ kiện không thể thiếu. Zenith sẽ mang đến bạn những mẫu túi treo đáng mua nhất hiện nay – bền bỉ, thẩm mỹ và tối ưu cho mọi hành trình.\r\n" },
        { "Giỏ xe đạp", "Giỏ xe đạp không chỉ là phụ kiện tiện lợi giúp bạn mang theo đồ đạc, mà còn thể hiện phong cách cá nhân và nâng cao trải nghiệm đạp xe hàng ngày. Zenith cung cấp đa dạng các loại giỏ xe đạp từ các thương hiệu uy tín như B'TWIN, ELOPS, ROCKRIDER, đáp ứng mọi nhu cầu từ đi lại hàng ngày đến du lịch dài ngày.\r\n" },
        { "Chuông xe đạp", "Chuông xe đạp được coi là một phương thức giao tiếp khi tham gia giao thông, đặc biệt quen thuộc với những người yêu thích đạp xe.\r\n" },
        { "Yên xe đạp", "Yên xe đạp phù hợp sẽ giúp bạn tận hưởng mỗi chuyến đi một cách thoải mái và bảo vệ sức khỏe lâu dài. Với vô số lựa chọn trên thị trường, việc chọn đúng loại yên xe không phải điều dễ dàng. Hãy cùng khám phá những sản phẩm nổi bật từ Zenith.\r\n" },
        { "Quần Đạp Xe", "Tối ưu hiệu suất đạp xe với những chiếc quần đạp xe chuyên dụng từ Zenith! Dù bạn là người mới bắt đầu hay tay đua chuyên nghiệp, bộ sưu tập quần đạp xe nam nữ tại đây sẽ mang đến cho bạn cảm giác êm ái, thoáng khí, chống ma sát tốt và phù hợp với mọi địa hình: từ đường trường, leo núi đến đạp xe hàng ngày\r\n" },
        { "Áo Đạp Xe", "Với thiết kế chuyên dụng giúp thoáng khí, co giãn, thấm hút mồ hôi tốt, áo đạp xe từ Decatlon giúp bạn luôn thoải mái dù đạp xe đường trường, leo đèo hay đi dạo phố. Danh mục này tổng hợp các mẫu áo xe đạp cho mọi nhu cầu – từ người mới bắt đầu đến vận động viên chuyên nghiệp.\r\n" },
        { "Lốp Xe", "Tại Zenith, bạn dễ dàng tìm thấy đa dạng mẫu lốp xe đạp chính hãng, phù hợp cho mọi dòng xe và nhu cầu sử dụng. Từ lốp xe đạp trẻ em, xe đạp đường phố đến lốp địa hình, tất cả đều được chọn lọc kỹ càng, đảm bảo độ bền, khả năng chống mài mòn và an toàn khi vận hành.\r\n" },
        { "Săm Xe", "Săm xe đạp là bộ phận quan trọng ảnh hưởng trực tiếp đến trải nghiệm lái và sự an toàn của bạn trên mọi cung đường. Trên thị trường hiện nay có rất nhiều loại săm với kích thước và chất liệu khác nhau, phù hợp với từng dòng xe đạp. Cùng khám phá các sản phẩm săm xe đạp tại Zenith bền đẹp, an toàn.\r\n" },
        { "Phanh Xe", "Phanh xe đạp không chỉ ảnh hưởng trực tiếp đến hiệu suất mà còn quyết định sự an toàn của bạn trên mọi cung đường. Việc lựa chọn một bộ phanh chất lượng, bền bỉ và hiệu quả là điều cần thiết cho cả những tay đua chuyên nghiệp lẫn người đạp xe hàng ngày. Khám phá các sản phẩm phanh xe đạp Zenith đáng tin cậy, chất lượng cao.\r\n" },
        { "Tay Lái & Pô-tăng", "Việc chọn tay lái xe đạp và pô-tăng không chỉ dừng lại ở kiểu dáng hay tính năng, mà quan trọng hơn là phải phù hợp với mục đích sử dụng của bạn – từ đạp phố, đi tour cho đến địa hình gồ ghề. Cùng Zenith khám phá những tiêu chí quan trọng khi lựa chọn, đồng thời gợi ý các loại tay lái phù hợp với từng nhu cầu cụ thể.\r\n" },
        { "Bàn Đạp", "Bàn đạp là một trong những bộ phận quan trọng ảnh hưởng trực tiếp đến hiệu suất và trải nghiệm khi đạp xe. Tại Zenith – cửa hàng phụ kiện xe đạp uy tín đến từ Pháp, bạn sẽ dễ dàng tìm thấy hàng chục mẫu bàn đạp chất lượng, đáp ứng mọi nhu cầu từ cơ bản đến chuyên nghiệp, đi kèm chính sách mua sắm tiện lợi và bảo hành rõ ràng.\r\n" },
        { "Vợt cầu lông & quả cầu lông", "Để việc luyện tập và thi đấu cầu lông đạt hiệu quả cao, một chiếc vợt phù hợp là điều vô cùng quan trọng. Hiểu được nhu cầu đó, Zenith - thương hiệu chuyên cung cấp đồ thể thao uy tín từ Pháp tự hào giới thiệu 50+ mẫu vợt cầu lông chất lượng cao cho cả người lớn và trẻ em với mức giá chỉ từ 175,000 VND.\r\n" },
        { "Giày & tất cầu lông", "Bạn cần tìm giày cầu lông chính hãng, chất lượng cao và không ? Zenith luôn có sẵn hơn 20 mẫu giày cầu lông đáp ứng mọi nhu cầu, từ mới chơi đến trình độ chuyên nghiệp. Giày đánh cầu lông đẹp với các công nghệ độc quyền giúp bạn luyện tập hiệu quả. Mua ngay giày cầu lông chỉ từ 495K.\r\n" },
        { "Quần áo cầu lông", "Cầu lông không chỉ là môn thể thao phổ biến, mang tính rèn luyện sức khỏe, mà còn là niềm đam mê của nhiều người ở mọi lứa tuổi. Để có trải nghiệm thi đấu và tập luyện thoải mái, trang phục đóng vai trò rất quan trọng. Quần áo cầu lông cần đáp ứng cả về chất liệu, thiết kế lẫn độ bền để người chơi tự tin trong từng cú đánh\r\n" },
        { "Vợt Tennis & Bóng", "Một chiếc vợt Tennis chất lượng sẽ tăng hiệu suất chơi và nâng cao trình độ của bạn hiệu quả hơn. Hiểu được đó, Zenith mang đến cho bạn đa dạng lựa chọn với hơn 50++ mẫu vợt Tennis chính hãng, chất lượng với giá tốt chỉ từ chất lượng giá tốt chỉ từ 295,000 VND.\r\n" },
        { "Trang phục tennis", "Để có thể tự tin chinh phục những trái bóng, bạn cần trang bị cho mình những bộ trang phục tennis vừa thoải mái, vừa thời trang. Zenith xin giới thiệu tới bạn từ những chiếc áo thun thoáng mát, những chiếc quần short năng động đến các phụ kiện cần thiết khác như băng buộc đầu, mũ lưỡi trai,... Đặt mua ngay!\r\n" },
        { "Vợt bóng bàn", "Bạn là tín đồ bóng bàn, cần tìm mua một cây vợt bóng bàn phù hợp để nâng cao kỹ năng bản thân thì các mẫu sản phẩm sau đây sẽ giúp đáp ứng mọi nhu cầu của bạn. Chúng tôi cung cấp đa dạng các loại vợt bóng bàn phù hợp với trình độ của mỗi người, giúp bạn luyện tập hiệu quả. Mua ngay vợt bóng bàn của Zenith chỉ từ 69k!\r\n" },
        { "Vợt pickleball", "Pickleball đang ngày càng trở thành môn thể thao được yêu thích nhờ sự kết hợp thú vị giữa quần vợt, bóng bàn và cầu lông. Để bắt đầu tập luyện và nâng cao kỹ năng, việc lựa chọn một cây vợt phù hợp là yếu tố quan trọng. Tại Zenith, bạn có thể dễ dàng tìm thấy những mẫu vợt pickleball chính hãng với thiết kế hiện đại, chất liệu bền bỉ và giá cả hợp lý, đáp ứng nhu cầu từ người mới tập cho đến vận động viên chơi lâu năm.\r\n" },
        { "Quần áo & phụ kiện Pickleball", "Pickleball đang trở thành môn thể thao yêu thích của nhiều người bởi tính vui nhộn, vận động toàn thân và dễ chơi. Một bộ quần áo phù hợp không chỉ mang lại sự thoải mái khi di chuyển mà còn giúp bạn tự tin, nổi bật trên sân.\r\n" },
        { "Quả Bóng Đá", "Bóng đá là môn thể thao đòi hỏi nhiều vận động, vì vậy một quả bóng phù hợp sẽ giúp trải nghiệm trên sân thêm hứng khởi. Tại Zenith, bạn dễ dàng tìm thấy bóng chính hãng, bền đẹp và đa dạng mẫu mã, phù hợp cho cả trẻ em, người mới bắt đầu lẫn người chơi phong trào.\r\n" },
        { "Quả bóng rổ", "Từ những quả bóng rổ chuyên nghiệp theo tiêu chuẩn FIBA đến những quả bóng dành cho trẻ em mới bắt đầu tập luyện, Zenith đều có sẵn. Với thiết kế đa dạng, chất lượng đảm bảo và giá cả phải chăng, quả bóng rổ Zenith sẽ là người bạn đồng hành lý tưởng trên sân đấu.\r\n" },
        { "Giày bóng rổ", "Giày bóng rổ không chỉ là phụ kiện thi đấu, mà còn là yếu tố then chốt giúp người chơi di chuyển linh hoạt, bật nhảy mạnh mẽ và hạn chế chấn thương. Tại Zenith, bạn dễ dàng tìm thấy những đôi giày bóng rổ thiết kế chuyên biệt cho từng trình độ, từ người mới tập đến vận động viên chuyên nghiệp. \r\n" },
        { "Quần Áo bóng rổ", "Quần áo bóng rổ không chỉ là trang phục mà còn là yếu tố quan trọng giúp vận động viên thoải mái, tự tin thể hiện kỹ năng trên sân. Tại Zenith, các mẫu quần áo bóng rổ nam, nữ được thiết kế đa dạng về kiểu dáng, màu sắc, từ áo thun, áo tank top, quần short đến bộ set đồng phục, đáp ứng nhu cầu luyện tập và thi đấu ở mọi cấp độ.\r\n" }


    };


    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await context.Database.MigrateAsync();

        var now = DateTime.Now;

        // ====================================================================
        // 💡 1. SEEDING SUPPLIER (ĐẢM BẢO KHÓA NGOẠI CHO PRODUCTS)
        // ====================================================================
        if (!await context.Suppliers.AnyAsync())
        {
            var supplier = new Supplier
            {
                SupplierName = "Zenith",
                IsActive = true,
                CreatedAt = now
            };
            context.Suppliers.Add(supplier);
            // Cần lưu ngay lập tức để SupplierId được gán và có thể được dùng cho Products
            await context.SaveChangesAsync();
        }

        // Lấy SupplierId mặc định sau khi đã đảm bảo nó tồn tại
        var defaultSupplierId = await context.Suppliers
            .Where(s => s.SupplierName == "Zenith")
            .Select(s => s.SupplierId)
            .FirstOrDefaultAsync();

        if (defaultSupplierId == 0) return; // Thoát nếu vẫn không tìm thấy (tránh lỗi)


        // ====================================================================
        // 2. IDENTITY SEEDING (ROLES & ADMIN)
        // ====================================================================

        // --- TẠO ROLES ---
        var roles = new ApplicationRole[]
        {
            new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN", Description = "Quản trị viên hệ thống" },
            new ApplicationRole { Name = "Customer", NormalizedName = "CUSTOMER", Description = "Khách hàng mua sắm" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }

        // --- TẠO TÀI KHOẢN ADMIN ---
        const string adminEmail = "admin@zenith.com";
        const string adminPass = "Admin@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin ZENITH",
                EmailConfirmed = true,
                CreatedAt = now
            };
            var result = await userManager.CreateAsync(adminUser, adminPass);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // ====================================================================
        // 3. SPORTS, CATEGORIES & LINKING SEEDING (IDEMPOTENT)
        // ====================================================================
        {
            // Mảng dữ liệu SPORTS/CATEGORIES của bạn
            var sportData = new (string ParentName, string SportName, string[] Categories)[]
            {
                // --- LEO NÚI & DÃ NGOẠI ---
                (null, "Leo Núi & Dã Ngoại", Array.Empty<string>()),
                ("Leo Núi & Dã Ngoại", "Hiking & Trekking", new[] { "Balo & Túi", "Quần Áo Leo Núi", "Giày Leo Núi", "Phụ Kiện Leo Núi", "Vớ Leo Núi" }),
                ("Leo Núi & Dã Ngoại", "Cắm Trại", new[] { "Lều Cắm Trại", "Túi Ngủ & Đệm Hơi", "Ghế Cắm Trại", "Bàn Cắm Trại", "Dụng Cụ Nấu Ăn", "Đèn Cắm Trại", "Vệ Sinh Cá Nhân" }),
                ("Leo Núi & Dã Ngoại", "Leo Núi Nhân Tạo", new[] { "Thiết Bị Leo Núi Nhân Tạo", "Giày & Mũ Bảo Hiểm Leo Núi Nhân Tạo", "Phụ Kiện Leo Núi Nhân Tạo" }),
                
                // --- CHẠY BỘ & ĐI BỘ ---
                (null, "Chạy Bộ & Đi Bộ", Array.Empty<string>()),
                ("Chạy Bộ & Đi Bộ", "Chạy Bộ Đường Trường", new[] { "Giày Chạy Bộ", "Quần Áo Chạy Bộ Nam", "Quần Áo Chạy Bộ Nữ", "Phụ Kiện Chạy Bộ", "Chạy Bộ Mùa Lạnh", "Dinh Dưỡng, Băng Gối & Dụng Cụ Massage" }),
                ("Chạy Bộ & Đi Bộ", "Chạy Địa Hình (Trail)", new[] { "Quần Áo Chạy Trail", "Giày Chạy Trail", "Túi Nước & Đai Chạy Địa Hình", "Phụ Kiện Chạy Trail" }),
                ("Chạy Bộ & Đi Bộ", "Đi Bộ", new[] { "Giày Đi Bộ", "Phụ Kiện Đi Bộ", "Tất Đi Bộ" }),

                // --- YOGA & PILATES ---
                (null, "Yoga & Pilates", Array.Empty<string>()),
                ("Yoga & Pilates", "Quần Áo Yoga & Pilates", new[] { "Áo Tập Yoga", "Áo Ngực Tập Yoga", "Quần Tập Yoga", "Quần Legging Yoga", "Tất Chống Trượt", "Đồ tập yoga nam", "Đồ tập yoga nữ" }),
                ("Yoga & Pilates", "Thảm yoga pilates & túi đựng thảm", new[] { "Thảm yoga & Pilates", "Túi đựng thảm yoga" }),

                // --- BƠI LỘI ---
                (null, "Bơi Lội", Array.Empty<string>()),
                ("Bơi Lội", "Đồ bơi", new[] { "Đồ bơi nữ", "Đồ bơi nam" }),
                ("Bơi Lội", "Kính bơi", new[] { "Kính Bơi Người Lớn", "Kính Bơi Cận" }),
                ("Bơi Lội", "Mũ Bơi", new[] { "Mũ Bơi Người Lớn" }),
                ("Bơi Lội", "Phụ Kiện bơi", new[] { "Phao Bơi", "Khăn tắm", "Chân vịt bơi", "Kẹp Mũi & Bịt Tai", "Sữa Tắm & Kem Chống Nắng", "Bể Bơi Tại Nhà" }),
                
                // --- VÕ THUẬT TỔNG HỢP ---
                (null, "Võ Thuật Tổng Hợp", Array.Empty<string>()),
                ("Võ Thuật Tổng Hợp", "Boxing & Muay Thai", new[] { "Găng Tay Boxing", "Túi Cát & Đệm Boxing", "Bảo Vệ Hàm Boxing", "Dây Quấn Tay", "Đồ Bảo Vệ", "Quần Áo & Giày Boxing", "Phụ Kiện" }),
                
                // --- ĐẠP XE ---
                (null, "Đạp Xe", Array.Empty<string>()),
                ("Đạp Xe", "Xe đạp", new[] { "Xe Đạp Đường Trường", "Xe Đạp Địa Hình & Hybrid", "Xe Đạp Gấp", "Xe Đạp Thành Phố" }),
                ("Đạp Xe", "Phụ kiện xe đạp", new[] { "Đèn Xe Đạp", "Khóa xe đạp", "Túi xe đạp", "Giỏ xe đạp", "Chuông xe đạp", "Ba-ga xe đạp", "Yên xe đạp" }),
                ("Đạp Xe", "Quần Áo Đạp Xe", new[] { "Quần Đạp Xe", "Áo Đạp Xe", "Áo khoác đạp xe" }),
                ("Đạp Xe", "Phụ Tùng & Bảo Dưỡng", new[] { "Lốp Xe", "Săm Xe", "Linh Kiện Xe Đạp", "Phanh Xe", "Tay Lái & Pô-tăng", "Bàn Đạp", "Dụng Cụ Bảo Dưỡng" }),

                // --- THỂ THAO DÙNG VỢT ---
                (null, "Thể Thao Dùng Vợt", Array.Empty<string>()),
                ("Thể Thao Dùng Vợt", "Cầu Lông", new[] { "Vợt cầu lông & quả cầu lông", "Giày & tất cầu lông", "Quần áo cầu lông", "Phụ kiện cầu lông", "Lưới cầu lông" }),
                ("Thể Thao Dùng Vợt", "TENNIS", new[] { "Vợt Tennis & Bóng", "Trang phục tennis", "Giày & Tất Tennis", "Phụ kiện Tennis" }),
                ("Thể Thao Dùng Vợt", "Bóng Bàn", new[] { "Vợt bóng bàn", "Bàn & Lưới Bóng Bàn", "Trang phục bóng bàn", "Bao vợt" }),
                ("Thể Thao Dùng Vợt", "Pickleball", new[] { "Vợt pickleball", "Quần áo & phụ kiện Pickleball", "Phụ kiện pickleball" }),
                
                // --- THỂ THAO ĐỒNG ĐỘI ---
                (null, "Thể Thao Đồng Đội", Array.Empty<string>()),
                ("Thể Thao Đồng Đội", "Bóng Đá & Futsal", new[] { "Giày Bóng Đá & Futsal", "Quả Bóng Đá", "Quần Áo Bóng Đá", "Thiết Bị & Phụ Kiện Bóng Đá" }),
                ("Thể Thao Đồng Đội", "Bóng rổ", new[] { "Quả bóng rổ", "Giày bóng rổ", "Quần Áo bóng rổ", "Dụng cụ & Phụ Kiện Bóng rổ" }),
                ("Thể Thao Đồng Đội", "Bóng chuyền", new[] { "Bóng, Lưới & Dụng Cụ", "Quần Áo Bóng Chuyền", "Phụ Kiện Bóng Chuyền" }),
                ("Thể Thao Đồng Đội", "Bóng chày", new[] { "Gậy & Quả bóng chày", "Găng tay bóng chày" }),
            };

            // Dùng Dictionary để lưu các Category mới được tạo
            var seededCategories = new Dictionary<string, Category>();

            // --- 3.1 TẠO/ĐẢM BẢO CATEGORY (CÓ ImageUrl và Description) ---
            var existingCategories = await context.Categories
                .ToDictionaryAsync(c => c.CategoryName, c => c);

            int categoryImageIndex = 1;

            foreach (var item in sportData)
            {
                foreach (var categoryName in item.Categories)
                {
                    // TÌM DESCRIPTION TỪ DICTIONARY HOẶC DÙNG MẶC ĐỊNH
                    CategoryDescriptions.TryGetValue(categoryName, out var description);

                    if (!existingCategories.ContainsKey(categoryName))
                    {
                        // LOGIC TẠO ImageUrl: category-{index}.avif
                        var imageUrl = $"~/image/categoryImage/category/category-{categoryImageIndex}.avif";

                        var category = new Category
                        {
                            CategoryName = categoryName,
                            Description = description ?? $"Danh mục sản phẩm {categoryName} cho môn thể thao hiện đại.",
                            ImageUrl = imageUrl,
                            IsActive = true,
                            DisplayOrder = categoryDisplayOrder++,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        context.Categories.Add(category);
                        existingCategories[categoryName] = category;

                        categoryImageIndex++;
                    }
                    else
                    {
                        // Cập nhật Description và ImageUrl nếu Category đã tồn tại
                        var existingCategory = existingCategories[categoryName];
                        var imageUrl = $"~/image/categoryImage/category/category-{categoryImageIndex}.avif";

                        if (existingCategory.Description != description)
                        {
                            existingCategory.Description = description ?? $"Danh mục sản phẩm {categoryName} cho môn thể thao hiện đại.";
                            context.Categories.Update(existingCategory);
                        }
                        if (existingCategory.ImageUrl != imageUrl)
                        {
                            existingCategory.ImageUrl = imageUrl;
                            context.Categories.Update(existingCategory);
                        }

                        categoryImageIndex++;
                    }
                }
            }
            await context.SaveChangesAsync();

            // --- 3.2 TẠO/ĐẢM BẢO SPORTS (CÓ IconUrl) ---
            var existingSports = await context.Sports
                .Include(s => s.ParentSport)
                .ToDictionaryAsync(s => s.SportName, s => s);

            foreach (var item in sportData)
            {
                Sport? parent = null;
                if (!string.IsNullOrEmpty(item.ParentName))
                {
                    existingSports.TryGetValue(item.ParentName!, out parent);
                    if (parent == null)
                    {
                        SportIcons.TryGetValue(item.ParentName!, out var parentIconUrl);
                        parent = new Sport
                        {
                            SportName = item.ParentName!,
                            Description = item.ParentName!,
                            IconUrl = parentIconUrl,
                            IsActive = true,
                            DisplayOrder = sportDisplayOrder++,
                            CreatedAt = now
                        };
                        context.Sports.Add(parent);
                        await context.SaveChangesAsync();
                        existingSports[item.ParentName!] = parent;
                    }
                }

                // Lấy IconUrl cho Sport hiện tại
                SportIcons.TryGetValue(item.SportName, out var iconUrl);

                if (!existingSports.ContainsKey(item.SportName))
                {

                    var sport = new Sport
                    {
                        SportName = item.SportName,
                        Description = item.SportName,
                        ParentSport = parent,
                        IconUrl = iconUrl,
                        IsActive = true,
                        DisplayOrder = sportDisplayOrder++,
                        CreatedAt = now
                    };
                    context.Sports.Add(sport);
                    await context.SaveChangesAsync();
                    existingSports[item.SportName] = sport;
                }
                else if (existingSports.TryGetValue(item.SportName, out var existingSport))
                {
                    // CẬP NHẬT IconUrl nếu Sports đã tồn tại
                    if (existingSport.IconUrl != iconUrl)
                    {
                        existingSport.IconUrl = iconUrl;
                        context.Sports.Update(existingSport);
                    }
                }
            }
            await context.SaveChangesAsync(); // Lưu thay đổi IconUrl (nếu có)


            // --- 3.3 TẠO/ĐẢM BẢO LIÊN KẾT SPORT-CATEGORY ---
            var existingLinks = new HashSet<(int SportId, int CategoryId)>(
                (await context.SportCategories
                    .AsNoTracking()
                    .Select(sc => new { sc.SportId, sc.CategoryId })
                    .ToListAsync())
                .Select(x => (x.SportId, x.CategoryId))
            );

            foreach (var item in sportData)
            {
                var sport = existingSports[item.SportName];
                foreach (var categoryName in item.Categories)
                {
                    var category = existingCategories[categoryName];
                    var key = (sport.SportId, category.CategoryId);
                    if (!existingLinks.Contains(key))
                    {
                        context.SportCategories.Add(new SportCategory
                        {
                            SportId = sport.SportId,
                            CategoryId = category.CategoryId
                        });
                        existingLinks.Add(key);
                    }
                }
            }
            await context.SaveChangesAsync();
        }
        //
        // ====================================================================
        // 4. PRODUCT, VARIANT & IMAGE SEEDING
        // ====================================================================
        if (!context.Products.Any())
        {
            // 4.1. Tải các Khóa ngoại cần thiết (Foreign Keys)
            var allCategories = await context.Categories.ToDictionaryAsync(c => c.CategoryName);
            var allSports = await context.Sports.ToDictionaryAsync(s => s.SportName);
            // SupplierId đã được lấy ở đầu hàm: defaultSupplierId

            var productsToSeed = new List<Product>();
            var variantsToSeed = new List<ProductVariant>();
            var imagesToSeed = new List<ProductImage>();

            foreach (var prod in productData)
            {
                if (!allCategories.ContainsKey(prod.CategoryName) || !allSports.ContainsKey(prod.SportName))
                {
                    // Bỏ qua nếu Category hoặc Sport không tìm thấy (giúp tránh lỗi FK)
                    continue;
                }

                var category = allCategories[prod.CategoryName];
                var sport = allSports[prod.SportName];

                // 4.2. TẠO PRODUCT (BẢNG CHA)
                var newProduct = new Product
                {
                    CategoryId = category.CategoryId,
                    SportId = sport.SportId,
                    SupplierId = defaultSupplierId, // SỬ DỤNG ID ĐÃ ĐƯỢC ĐẢM BẢO TỒN TẠI
                    ProductName = prod.Name,
                    Description = prod.Desc,
                    Sku = prod.SkuBase,
                    IsActive = true,
                    IsFeatured = prod.IsFeat,
                    ViewCount = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                productsToSeed.Add(newProduct);
                productIndex++;

                // 4.3. TẠO PRODUCT VARIANTS (BẢNG CON)
                foreach (var variantData in prod.Variants)
                {
                    var newVariant = new ProductVariant
                    {
                        // EF Core sẽ tự gán ProductId vì có tham chiếu đến newProduct
                        Product = newProduct,
                        VariantSku = variantData.Sku,
                        Price = variantData.Price,
                        SalePrice = variantData.SalePrice,
                        StockQuantity = variantData.Stock,
                        LowStockThreshold = 10,
                        IsActive = true,
                        SoldCount = 0,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Attributes = variantData.Attributes
                    };
                    variantsToSeed.Add(newVariant);
                }

                // 4.4. TẠO PRODUCT IMAGES (BẢNG CON)
                for (int i = 1; i <= prod.ImageCount; i++)
                {
                    imagesToSeed.Add(new ProductImage
                    {
                        Product = newProduct, // EF Core sẽ tự gán ProductId
                        ImageUrl = $"~/image/productImage/{prod.SkuBase}/{prod.SkuBase}-{i}.avif",
                        IsPrimary = (i == 1),
                        DisplayOrder = i,
                    });
                }
            }
            // LƯU CÁC BẢNG CHA (Products) TRƯỚC
            context.Products.AddRange(productsToSeed);
            await context.SaveChangesAsync();

            // LƯU CÁC BẢNG CON (ProductVariants và ProductImages) SAU
            context.ProductVariants.AddRange(variantsToSeed);
            context.ProductImages.AddRange(imagesToSeed);

            await context.SaveChangesAsync();
        }
    }
}