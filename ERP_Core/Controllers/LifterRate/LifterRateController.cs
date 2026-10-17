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

namespace ERP_Core.Controllers.LifterRate
{
    /// <summary>
    /// Lifter / forklift rate master — peak vs normal time rates per equipment.
    /// </summary>
    [Authorize]
    [FormAccessAuthorize("Lifter Rates")]
    [Route("LifterRate")]
    public class LifterRateController : Controller
    {
        private readonly ERPDbContext _context;
        private readonly ILogger<LifterRateController> _logger;

        public LifterRateController(ERPDbContext context, ILogger<LifterRateController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetCurrentCompanyId()
        {
            return HttpContext.Session.GetInt32("CompanyId") ?? 1;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Lifter Rates";
            ViewData["SubTitle"] = "Configure standard normal and peak hourly rates for lifters and forklifts.";
            ViewData["ActivePage"] = "LifterRates";

            int companyId = GetCurrentCompanyId();

            var rates = await _context.LifterRates
                .Where(r => r.CompanyId == null || r.CompanyId == companyId)
                .OrderBy(r => r.EquipmentName)
                .AsNoTracking()
                .ToListAsync();

            return View(rates);
        }

        [HttpPost]
        [Route("Save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] Models.LifterRate model)
        {
            int companyId = GetCurrentCompanyId();
            string userName = User.Identity?.Name ?? "Admin";

            try
            {
                if (string.IsNullOrWhiteSpace(model.EquipmentName))
                {
                    TempData["ErrorMessage"] = "Equipment name is required.";
                    return RedirectToAction(nameof(Index));
                }

                if (model.Id > 0)
                {
                    var existing = await _context.LifterRates.FindAsync(model.Id);
                    if (existing == null)
                    {
                        TempData["ErrorMessage"] = "Rate not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    existing.EquipmentName = model.EquipmentName.Trim();
                    existing.NormalRate = model.NormalRate;
                    existing.PeakRate = model.PeakRate;
                    existing.PeakStartTime = model.PeakStartTime;
                    existing.PeakEndTime = model.PeakEndTime;
                    existing.Remarks = model.Remarks?.Trim();
                    existing.Active = model.Active ?? true;
                    existing.ModifiedBy = userName;
                    existing.ModifiedOn = DateTime.Now;

                    TempData["SuccessMessage"] = $"Lifter rate for '{existing.EquipmentName}' updated successfully.";
                }
                else
                {
                    model.EquipmentName = model.EquipmentName.Trim();
                    model.Remarks = model.Remarks?.Trim();
                    model.CompanyId = companyId;
                    model.Active = true;
                    model.CreatedBy = userName;
                    model.CreatedOn = DateTime.Now;

                    await _context.LifterRates.AddAsync(model);
                    TempData["SuccessMessage"] = $"Lifter rate for '{model.EquipmentName}' added successfully.";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving lifter rate");
                TempData["ErrorMessage"] = "Error saving lifter rate: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _context.LifterRates.FindAsync(id);
            if (r == null) return NotFound();

            return Json(new
            {
                id = r.Id,
                equipmentName = r.EquipmentName,
                normalRate = r.NormalRate,
                peakRate = r.PeakRate,
                peakStartTime = r.PeakStartTime?.ToString(@"hh\:mm") ?? "",
                peakEndTime = r.PeakEndTime?.ToString(@"hh\:mm") ?? "",
                remarks = r.Remarks ?? ""
            });
        }

        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var r = await _context.LifterRates.FindAsync(id);
                if (r == null)
                {
                    TempData["ErrorMessage"] = "Rate not found.";
                    return RedirectToAction(nameof(Index));
                }

                r.Active = false;
                r.ModifiedBy = User.Identity?.Name ?? "Admin";
                r.ModifiedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"'{r.EquipmentName}' deactivated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lifter rate");
                TempData["ErrorMessage"] = "Error deleting lifter rate: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
