using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSClean.Models
{
    public enum PaymentStatus
    {
        Pending,    // قيد الانتظار
        Completed,  // تم الدفع
        Failed,     // فشل الدفع
        Refunded    // تم الاسترداد
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}