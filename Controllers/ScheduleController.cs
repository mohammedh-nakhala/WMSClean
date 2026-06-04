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
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public ScheduleController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> MyRequests()
        {
            var userId = _userManager.GetUserId(User);
            var myRequests = await _context.Schedules
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.RequestedDate)
                .ToListAsync();
            return View(myRequests);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (string.IsNullOrEmpty(schedule.ProductName))
            {
                ModelState.AddModelError("ProductName", "Product name is required");
                ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
                return View(schedule);
            }

            schedule.UserId = _userManager.GetUserId(User);
            schedule.Status = ScheduleStatus.Pending;
            schedule.RequestedDate = DateTime.Now;

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                schedule.UserId,
                "📋 Request Submitted",
                $"Your storage request for '{schedule.ProductName}' has been submitted.",
                "Info",
                "/Schedule/MyRequests"
            );

            TempData["Success"] = "✓ Your request has been submitted successfully!";
            return RedirectToAction(nameof(MyRequests));
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AllRequests()
        {
            var allRequests = await _context.Schedules
                .Include(s => s.Warehouse)
                .OrderByDescending(s => s.RequestedDate)
                .ToListAsync();
            return View(allRequests);
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Approve(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null) return NotFound();

            var currentWeight = await _context.Products
                .Where(p => p.WarehouseId == schedule.WarehouseId && p.Status != "Exited")
                .SumAsync(p => p.WeightKg);

            var currentCount = await _context.Products
                .Where(p => p.WarehouseId == schedule.WarehouseId && p.Status != "Exited")
                .CountAsync();

            if (currentWeight + schedule.WeightKg > schedule.Warehouse.MaxWeightKg ||
                currentCount + 1 > schedule.Warehouse.MaxItems)
            {
                TempData["Error"] = "⚠️ Cannot approve! Warehouse capacity exceeded.";
                return RedirectToAction(nameof(AllRequests));
            }

            var product = new Product
            {
                Name = schedule.ProductName,
                WeightKg = schedule.WeightKg,
                SenderEmail = schedule.SenderEmail,
                ReceiverEmail = schedule.ReceiverEmail,
                WarehouseId = schedule.WarehouseId,
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                TrackingNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                EntryDate = DateTime.Now,
                Status = "In Warehouse",
                LengthCm = 50,
                WidthCm = 40,
                HeightCm = 30,
                ProductType = "General",
                PricePerCubicMeterPerDay = 5m
            };

            _context.Products.Add(product);
            schedule.Status = ScheduleStatus.Completed;
            schedule.ApprovedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                schedule.UserId,
                "✅ Request Approved",
                $"Your storage request for '{schedule.ProductName}' has been approved! Tracking: {product.TrackingNumber}",
                "Success",
                $"/Products/Details/{product.Id}"
            );

            TempData["Success"] = $"✓ Request for {schedule.ProductName} approved and converted to product!";
            return RedirectToAction(nameof(AllRequests));
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Reject(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                schedule.Status = ScheduleStatus.Rejected;
                await _context.SaveChangesAsync();

                await _notificationService.AddNotificationAsync(
                    schedule.UserId,
                    "❌ Request Rejected",
                    $"Your storage request for '{schedule.ProductName}' has been rejected.",
                    "Error",
                    null
                );

                TempData["Error"] = $"✗ Request for {schedule.ProductName} rejected.";
            }
            return RedirectToAction(nameof(AllRequests));
        }

      
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (schedule.UserId != userId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            if (status == "Cancel" && schedule.Status == ScheduleStatus.Pending)
            {
                schedule.Status = ScheduleStatus.Rejected;
                schedule.RejectionReason = "Cancelled by user";
                await _context.SaveChangesAsync();

                await _notificationService.AddNotificationAsync(
                    userId,
                    "📋 Request Cancelled",
                    $"Your storage request for '{schedule.ProductName}' has been cancelled.",
                    "Info",
                    "/Schedule/MyRequests"
                );

                TempData["Success"] = "Request cancelled successfully!";
            }

            return RedirectToAction(nameof(MyRequests));
        }
    }
}