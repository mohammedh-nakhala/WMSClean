using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSClean.Models
{
    public enum ScheduleStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }

    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public double WeightKg { get; set; }

        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ReceiverEmail { get; set; } = string.Empty;

        public DateTime RequestedDate { get; set; } = DateTime.Now;

        public DateTime? ApprovedDate { get; set; }

        [Required]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}