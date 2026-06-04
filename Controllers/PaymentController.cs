using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMSClean.Data;
using WMSClean.Models;
using WMSClean.Services;

namespace WMSClean.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;

        public PaymentController(ApplicationDbContext context, UserManager<IdentityUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // صفحة الدفع
        public async Task<IActionResult> Checkout(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var endDate = product.ExitDate ?? DateTime.Now;
            var totalDays = (endDate - product.EntryDate).TotalDays;
            if (totalDays < 1) totalDays = 1;

            var weightFactor = Math.Max(1, product.WeightKg / 50);
            var totalFee = (decimal)totalDays * product.PricePerCubicMeterPerDay * (decimal)product.VolumeCbm * (decimal)weightFactor;

            ViewBag.Product = product;
            ViewBag.TotalFee = totalFee;

            return View();
        }

        // معالجة الدفع
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int productId, string paymentMethod, string cardNumber, string expiryDate, string cvv)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var endDate = product.ExitDate ?? DateTime.Now;
            var totalDays = (endDate - product.EntryDate).TotalDays;
            if (totalDays < 1) totalDays = 1;

            var weightFactor = Math.Max(1, product.WeightKg / 50);
            var totalFee = (decimal)totalDays * product.PricePerCubicMeterPerDay * (decimal)product.VolumeCbm * (decimal)weightFactor;

            // محاكاة نجاح الدفع
            bool paymentSuccess = true;
            var userId = _userManager.GetUserId(User);

            var payment = new Payment
            {
                ProductId = productId,
                Amount = totalFee,
                PaymentMethod = paymentMethod,
                Status = paymentSuccess ? PaymentStatus.Completed : PaymentStatus.Failed,
                UserId = userId,
                Notes = $"Payment for product: {product.Name}"
            };

            _context.Payments.Add(payment);

            if (paymentSuccess)
            {
                await _notificationService.AddNotificationAsync(
                    userId,
                    "Payment Successful",
                    $"Your payment of {totalFee:C} for '{product.Name}' was successful.",
                    "Success",
                    $"/Payment/Receipt/{payment.Id}"
                );
            }

            await _context.SaveChangesAsync();

            if (paymentSuccess)
            {
                TempData["Success"] = "Payment completed successfully!";
                return RedirectToAction("Receipt", new { id = payment.Id });
            }
            else
            {
                TempData["Error"] = "Payment failed. Please try again.";
                return RedirectToAction("Checkout", new { productId });
            }
        }

        // إيصال الدفع
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // سجل المدفوعات
        public async Task<IActionResult> MyPayments()
        {
            var userId = _userManager.GetUserId(User);
            var payments = await _context.Payments
                .Include(p => p.Product)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }
    }
}