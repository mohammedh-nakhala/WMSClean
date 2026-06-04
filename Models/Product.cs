using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSClean.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Weight is required")]
        [Range(0.1, 10000, ErrorMessage = "Weight must be between 0.1 and 10000 kg")]
        public double WeightKg { get; set; }

        [Required(ErrorMessage = "Length is required")]
        [Range(1, 500, ErrorMessage = "Length must be between 1 and 500 cm")]
        public double LengthCm { get; set; } = 50;

        [Required(ErrorMessage = "Width is required")]
        [Range(1, 500, ErrorMessage = "Width must be between 1 and 500 cm")]
        public double WidthCm { get; set; } = 40;

        [Required(ErrorMessage = "Height is required")]
        [Range(1, 500, ErrorMessage = "Height must be between 1 and 500 cm")]
        public double HeightCm { get; set; } = 30;

        [NotMapped]
        public double AreaSqm => (LengthCm / 100) * (WidthCm / 100);

        [NotMapped]
        public double VolumeCbm => (LengthCm / 100) * (WidthCm / 100) * (HeightCm / 100);

        [Required(ErrorMessage = "Product type is required")]
        [RegularExpression("^(General|Cold|Hazardous|Fragile)$", ErrorMessage = "Invalid product type")]
        public string ProductType { get; set; } = "General";

        public DateTime EntryDate { get; set; } = DateTime.Now;

        public DateTime? ExitDate { get; set; }

        [Required(ErrorMessage = "Sender email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string SenderEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Receiver email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string ReceiverEmail { get; set; } = string.Empty;

        [Required]
        public string InvoiceNumber { get; set; } = string.Empty;

        public string TrackingNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        [Required(ErrorMessage = "Warehouse is required")]
        public int WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public int? StorageZoneId { get; set; }

        [ForeignKey("StorageZoneId")]
        public virtual StorageZone? StorageZone { get; set; }

        public string Status { get; set; } = "In Warehouse";

        [Required(ErrorMessage = "Storage price is required")]
        [Range(0.01, 1000, ErrorMessage = "Price must be between 0.01 and 1000 EGP")]
        public decimal PricePerCubicMeterPerDay { get; set; } = 5m;

        [NotMapped]
        public decimal TotalStorageFee
        {
            get
            {
                var endDate = ExitDate ?? DateTime.Now;
                var totalDays = (endDate - EntryDate).TotalDays;
                if (totalDays < 1) totalDays = 1;

                var volumeWeightFactor = VolumeCbm * (WeightKg / 100);
                if (volumeWeightFactor < 0.1) volumeWeightFactor = 0.1;

                return (decimal)totalDays * PricePerCubicMeterPerDay * (decimal)volumeWeightFactor;
            }
        }

        [NotMapped]
        public string FormattedStorageFee => TotalStorageFee.ToString("C");

        // حساب عدد الأيام المخزنة
        [NotMapped]
        public int TotalDaysStored
        {
            get
            {
                var endDate = ExitDate ?? DateTime.Now;
                var days = (endDate - EntryDate).Days;
                return days < 1 ? 1 : days;
            }
        }
    }
}