using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace  WMSClean.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int MaxItems { get; set; } = 1000;

        [Required]
        public double MaxWeightKg { get; set; } = 3000;

        public virtual ICollection<StorageZone> StorageZones { get; set; } = new List<StorageZone>();
    }
}