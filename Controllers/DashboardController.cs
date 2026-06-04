using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMSClean.Data;
using WMSClean.Models;

namespace WMSClean.Controllers
{
    [Authorize(Roles = "SuperAdmin")] 
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.Status == "In Warehouse");
            var exitedProducts = await _context.Products.CountAsync(p => p.Status == "Exited");
            var totalWarehouses = await _context.Warehouses.CountAsync();
            var totalStorageZones = await _context.StorageZones.CountAsync();
            var pendingSchedules = await _context.Schedules.CountAsync(s => s.Status == ScheduleStatus.Pending);

            var exitedProductsList = await _context.Products.Where(p => p.Status == "Exited").ToListAsync();
            double totalRevenue = 0;
            foreach (var product in exitedProductsList)
            {
                var endDate = product.ExitDate ?? DateTime.Now;
                var totalHours = (endDate - product.EntryDate).TotalHours;
                totalRevenue += totalHours * product.WeightKg * (double)product.PricePerCubicMeterPerDay;
            }

            var monthlyData = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var monthStart = new DateTime(date.Year, date.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var productsCount = await _context.Products.CountAsync(p => p.EntryDate >= monthStart && p.EntryDate <= monthEnd);
                monthlyData.Add(new { month = date.ToString("MMM yyyy"), count = productsCount });
            }

            ViewBag.MonthlyData = System.Text.Json.JsonSerializer.Serialize(monthlyData);
            ViewBag.TotalProducts = totalProducts;
            ViewBag.ActiveProducts = activeProducts;
            ViewBag.ExitedProducts = exitedProducts;
            ViewBag.TotalRevenue = totalRevenue.ToString("C");
            ViewBag.PendingSchedules = pendingSchedules;
            ViewBag.TotalWarehouses = totalWarehouses;
            ViewBag.TotalStorageZones = totalStorageZones;

            return View();
        }
    }
}