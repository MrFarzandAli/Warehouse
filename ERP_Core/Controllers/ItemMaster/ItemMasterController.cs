using System;
using System.Linq;
using System.Threading.Tasks;
using ERP_Core.Attributes;
using ERP_Core.Data;
using ERP_Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP_Core.Controllers.ItemMaster
{
    [Authorize]
    [FormAccessAuthorize("Item Master")]
    [Route("ItemMaster")]
    public class ItemMasterController : Controller
    {
        private readonly ERPDbContext _context;
        private readonly ILogger<ItemMasterController> _logger;

        public ItemMasterController(ERPDbContext context, ILogger<ItemMasterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetCurrentCompanyId()
        {
            return HttpContext.Session.GetInt32("CompanyId") ?? 1;
        }

        // =========================================================================
        // 1. LIST
        // =========================================================================
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? search, string? category)
        {
            ViewData["Title"] = "Item Master";
            ViewData["SubTitle"] = "Manage warehouse storage items and products used in inward stock and billing.";
            ViewData["ActivePage"] = "ItemMaster";

            int companyId = GetCurrentCompanyId();

            var query = _context.WarehouseItems
                .Where(i => i.CompanyId == null || i.CompanyId == companyId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    (i.Code != null && i.Code.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                query = query.Where(i => i.Category == category);

            var items = await query.OrderBy(i => i.Name).ToListAsync();

            ViewBag.Categories = await _context.WarehouseItems
                .Where(i => i.Category != null && i.Category != "")
                .Select(i => i.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCategory = category ?? "All";

            return View(items);
        }

        // =========================================================================
        // 2. SAVE (Create / Update)
        // =========================================================================
        [HttpPost]
        [Route("Save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] WarehouseItem model)
        {
            int companyId = GetCurrentCompanyId();
            string userName = User.Identity?.Name ?? "Admin";

            try
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    TempData["ErrorMessage"] = "Item Name is required.";
                    return RedirectToAction(nameof(Index));
                }

                if (model.Id > 0)
                {
                    var existing = await _context.WarehouseItems.FindAsync(model.Id);
                    if (existing == null)
                    {
                        TempData["ErrorMessage"] = "Item not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    existing.Code = model.Code?.Trim();
                    existing.Name = model.Name.Trim();
                    existing.Unit = string.IsNullOrWhiteSpace(model.Unit) ? "Bag" : model.Unit.Trim();
                    existing.Category = model.Category?.Trim();
                    existing.Description = model.Description?.Trim();
                    existing.Active = model.Active ?? true;
                    existing.ModifiedBy = userName;
                    existing.ModifiedOn = DateTime.Now;

                    TempData["SuccessMessage"] = $"Item '{existing.Name}' updated successfully.";
                }
                else
                {
                    // Auto code if empty: ITM-0001
                    if (string.IsNullOrWhiteSpace(model.Code))
                    {
                        int count = await _context.WarehouseItems.CountAsync();
                        model.Code = $"ITM-{(count + 1):D4}";
                    }

                    model.Name = model.Name.Trim();
                    model.Unit = string.IsNullOrWhiteSpace(model.Unit) ? "Bag" : model.Unit.Trim();
                    model.Category = model.Category?.Trim();
                    model.Description = model.Description?.Trim();
                    model.CompanyId = companyId;
                    model.Active = true;
                    model.CreatedBy = userName;
                    model.CreatedOn = DateTime.Now;

                    await _context.WarehouseItems.AddAsync(model);
                    TempData["SuccessMessage"] = $"Item '{model.Name}' added successfully.";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving warehouse item");
                TempData["ErrorMessage"] = "Error saving warehouse item: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 3. GET BY ID (for edit modal)
        // =========================================================================
        [HttpGet]
        [Route("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.WarehouseItems.FindAsync(id);
            if (item == null) return NotFound();

            return Json(new
            {
                id = item.Id,
                code = item.Code ?? "",
                name = item.Name,
                unit = item.Unit ?? "Bag",
                category = item.Category ?? "",
                description = item.Description ?? "",
                active = item.Active ?? true
            });
        }

        // =========================================================================
        // 4. DELETE (soft)
        // =========================================================================
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _context.WarehouseItems.FindAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Block delete if used in any lot
                bool inUse = await _context.WarehouseLots.AnyAsync(l => l.ProductId == id);
                if (inUse)
                {
                    TempData["ErrorMessage"] = $"Item '{item.Name}' is linked to active lots and cannot be deleted. Deactivate it instead.";
                    return RedirectToAction(nameof(Index));
                }

                item.Active = false;
                item.ModifiedBy = User.Identity?.Name ?? "Admin";
                item.ModifiedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Item '{item.Name}' deactivated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting warehouse item");
                TempData["ErrorMessage"] = "Error deleting warehouse item: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
