using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMSClean.Data;
using WMSClean.Models;

namespace WMSClean.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")] 
    public class StorageZoneController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StorageZoneController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var zones = await _context.StorageZones.Include(z => z.Warehouse).ToListAsync();
            return View(zones);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StorageZone zone)
        {
            if (ModelState.IsValid)
            {
                _context.StorageZones.Add(zone);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Storage zone created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
            return View(zone);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var zone = await _context.StorageZones.FindAsync(id);
            if (zone == null) return NotFound();
            ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
            return View(zone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StorageZone zone)
        {
            if (id != zone.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(zone);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StorageZones.Any(z => z.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
            return View(zone);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var zone = await _context.StorageZones.Include(z => z.Warehouse).FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null) return NotFound();
            return View(zone);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var zone = await _context.StorageZones.FindAsync(id);
            if (zone != null) _context.StorageZones.Remove(zone);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}