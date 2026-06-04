using Microsoft.AspNetCore.Mvc;

namespace WMSClean.Controllers
{
    public class SimpleWarehouseController : Controller
    {
        // Test action - returns text
        public string Test()
        {
            return "Warehouse Controller is working!";
        }

        // Index action - returns view
        public IActionResult Index()
        {
            return View();
        }
    }
}