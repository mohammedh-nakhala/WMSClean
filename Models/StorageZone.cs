using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSClean.Models
{
    public class StorageZone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ZoneName { get; set; } = string.Empty;

        // نوع المنطقة (يمكن استخدامها للفرز التلقائي)
        public string ZoneType { get; set; } = "General"; // General, Cold, Hazardous, Fragile

        // ★★★★★ مساحة المنطقة بالمتر المربع ★★★★★
        [Required]
        [Range(1, 10000)]
        public double AreaSqm { get; set; }

        // ★★★★★ الحد الأقصى للمساحة القابلة للاستخدام (نسبة من المساحة الكلية) ★★★★★
        [Required]
        [Range(1, 100)]
        public int MaxCapacityPercent { get; set; } = 100;

        // المساحة المستخدمة حالياً (تحتاج إلى تتبع المنتجات)
        public double CurrentUsedArea { get; set; } = 0;

        // عدد العناصر الحالي
        public int CurrentItemsCount { get; set; } = 0;

        // الوزن الحالي
        public double CurrentWeightKg { get; set; } = 0;

        // ★★★★★ سعر المنطقة لكل متر مكعب لكل يوم (جنيه مصري) ★★★★★
        [Required]
        [Range(0.01, 1000)]
        public decimal PricePerCubicMeterPerDay { get; set; } = 5m;

        // الارتفاع الافتراضي للمنطقة (لحساب الحجم)
        public double DefaultHeightMeters { get; set; } = 2.5;

        // حساب الحجم المتاح (مساحة × ارتفاع)
        public double GetAvailableVolume()
        {
            return AreaSqm * DefaultHeightMeters * (MaxCapacityPercent / 100.0);
        }

        // حساب الحجم المستخدم حالياً (تقديري حسب عدد العناصر)
        public double GetCurrentUsedVolume()
        {
            // تستطيع حساب الحجم بناءً على البيانات الواردة من المنتجات
            return (CurrentItemsCount * 0.1) * DefaultHeightMeters; // مثال تقريبي
        }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public int WarehouseId { get; set; }

        public virtual ICollection<Product>? Products { get; set; }
    }
}