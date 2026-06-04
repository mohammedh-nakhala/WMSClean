using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMSClean.Data;
using WMSClean.Models;
using WMSClean.Services;

namespace WMSClean.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")] 
    public class ProductController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public ProductController(ApplicationDbContext context, IEmailService emailService, UserManager<IdentityUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

      
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Warehouse)
                .Include(p => p.StorageZone)
                .ToListAsync();
            return View(products);
        }

 
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
            ViewBag.StorageZones = await _context.StorageZones.ToListAsync();
            return View();
        }

        
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,WeightKg,SenderEmail,ReceiverEmail,WarehouseId,StorageZoneId,PricePerCubicMeterPerDay,LengthCm,WidthCm,HeightCm,ProductType")] Product product)
        {
            if (string.IsNullOrEmpty(product.Name))
            {
                ModelState.AddModelError("Name", "Product name is required");
                ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
                ViewBag.StorageZones = await _context.StorageZones.ToListAsync();
                return View(product);
            }

            if (product.WeightKg <= 0)
            {
                ModelState.AddModelError("WeightKg", "Weight must be greater than 0");
                ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
                ViewBag.StorageZones = await _context.StorageZones.ToListAsync();
                return View(product);
            }

            if (product.WarehouseId == 0)
            {
                ModelState.AddModelError("WarehouseId", "Please select a warehouse");
                ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
                ViewBag.StorageZones = await _context.StorageZones.ToListAsync();
                return View(product);
            }

            
            if (product.PricePerCubicMeterPerDay <= 0)
            {
                product.PricePerCubicMeterPerDay = 5m;
            }

            bool isAvailable = await IsWarehouseAvailable(product.WarehouseId, product.WeightKg);

            if (!isAvailable)
            {
                
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                var schedule = new Schedule
                {
                    ProductName = product.Name,
                    WeightKg = product.WeightKg,
                    SenderEmail = product.SenderEmail,
                    ReceiverEmail = product.ReceiverEmail,
                    WarehouseId = product.WarehouseId,
                    UserId = userId,
                    Status = ScheduleStatus.Pending,
                    RequestedDate = DateTime.Now,
                    Notes = "تم تحويله تلقائياً لعدم وجود مساحة"
                };

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                await _notificationService.AddNotificationAsync(
                    userId,
                    "⚠️ Storage Request Created",
                    $"Your product '{product.Name}' has been converted to a schedule request.",
                    "Warning",
                    "/Schedule/MyRequests"
                );

                TempData["Message"] = "⚠️ Warehouse is full! A schedule request has been created.";
                return RedirectToAction(nameof(Index));
            }

          
            product.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
            product.TrackingNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            product.EntryDate = DateTime.Now;
            product.Status = "In Warehouse";

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                _userManager.GetUserId(User),
                "✅ Product Added Successfully",
                $"Your product '{product.Name}' has been added to warehouse. Tracking: {product.TrackingNumber}",
                "Success",
                $"/Products/Details/{product.Id}"
            );

            await UpdateStorageZoneCapacity(product.WarehouseId, product.WeightKg);

            try
            {
                await _emailService.SendProductReceivedEmailAsync(product.SenderEmail, product.Name);
            }
            catch { }

            TempData["Success"] = "✓ Product added successfully!";
            return RedirectToAction(nameof(Index));
        }

      
        [Route("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Warehouse)
                .Include(p => p.StorageZone)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

      
        [Route("PrintLabel/{id}")]
        public async Task<IActionResult> PrintLabel(int id)
        {
            var product = await _context.Products
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        
        [Route("ExitProduct/{id}")]
        public async Task<IActionResult> ExitProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.ExitDate = DateTime.Now;
                product.Status = "Exited";
                await _context.SaveChangesAsync();

                await _notificationService.AddNotificationAsync(
                    _userManager.GetUserId(User),
                    "📦 Product Exited",
                    $"Your product '{product.Name}' has been removed from the warehouse.",
                    "Info",
                    $"/Products/Details/{product.Id}"
                );

                try
                {
                    await _emailService.SendProductExitedEmailAsync(product.SenderEmail, product.Name);
                    await _emailService.SendShipmentOnWayEmailAsync(product.ReceiverEmail, product.Name, product.TrackingNumber);
                }
                catch { }
            }
            return RedirectToAction(nameof(Index));
        }

        [Route("Invoice/{id}")]
        public async Task<IActionResult> Invoice(int id)
        {
            var product = await _context.Products
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

           
            TempData["Info"] = "PDF invoice feature coming soon!";
            return RedirectToAction(nameof(Details), new { id });
        }

       
        [Route("FeeDetails/{id}")]
        public async Task<IActionResult> FeeDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Warehouse)
                .Include(p => p.StorageZone)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        //  التحقق من سعة 
        private async Task<bool> IsWarehouseAvailable(int warehouseId, double newWeight, int newItemsCount = 1)
        {
            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
            if (warehouse == null) return false;

            var currentTotalWeight = await _context.Products
                .Where(p => p.WarehouseId == warehouseId && p.Status != "Exited")
                .SumAsync(p => p.WeightKg);

            var currentItemCount = await _context.Products
                .Where(p => p.WarehouseId == warehouseId && p.Status != "Exited")
                .CountAsync();

            if (currentTotalWeight + newWeight > warehouse.MaxWeightKg || currentItemCount + newItemsCount > warehouse.MaxItems)
                return false;

            return true;
        }

        // تحديث سعة منطقة التخزين
        private async Task UpdateStorageZoneCapacity(int warehouseId, double weight)
        {
            var zone = await _context.StorageZones.FirstOrDefaultAsync(z => z.WarehouseId == warehouseId);
            if (zone != null)
            {
                zone.CurrentItemsCount++;
                zone.CurrentWeightKg += weight;
                await _context.SaveChangesAsync();
            }
        }
    }
}