using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMSClean.Data;

namespace WMSClean.Controllers
{
    [Authorize(Roles = "SuperAdmin")] // فقط Super Admin
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ProductsReport()
        {
            var products = await _context.Products.Include(p => p.Warehouse).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Products Report");
                worksheet.Cell(1, 1).Value = "Product Name";
                worksheet.Cell(1, 2).Value = "Weight (Kg)";
                worksheet.Cell(1, 3).Value = "Warehouse";
                worksheet.Cell(1, 4).Value = "Entry Date";
                worksheet.Cell(1, 5).Value = "Exit Date";
                worksheet.Cell(1, 6).Value = "Status";
                worksheet.Cell(1, 7).Value = "Sender Email";
                worksheet.Cell(1, 8).Value = "Receiver Email";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = product.Name;
                    worksheet.Cell(row, 2).Value = product.WeightKg;
                    worksheet.Cell(row, 3).Value = product.Warehouse?.Name ?? "N/A";
                    worksheet.Cell(row, 4).Value = product.EntryDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 5).Value = product.ExitDate?.ToString("yyyy-MM-dd HH:mm") ?? "Still in warehouse";
                    worksheet.Cell(row, 6).Value = product.Status;
                    worksheet.Cell(row, 7).Value = product.SenderEmail;
                    worksheet.Cell(row, 8).Value = product.ReceiverEmail;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Products_Report_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}