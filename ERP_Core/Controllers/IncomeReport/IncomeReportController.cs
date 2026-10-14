using System;
using System.Linq;
using System.Threading.Tasks;
using ERP_Core.Attributes;
using ERP_Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP_Core.Controllers.IncomeReport
{
    /// <summary>
    /// Warehouse Income Report — converted from the fish Sales Report.
    /// Shows rent + lifter income party-wise and date-wise from WarehouseRentBills.
    /// </summary>
    [Authorize]
    [FormAccessAuthorize("Income Report")]
    [Route("IncomeReport")]
    public class IncomeReportController : Controller
    {
        private readonly ERPDbContext _context;
        private readonly ILogger<IncomeReportController> _logger;

        public IncomeReportController(ERPDbContext context, ILogger<IncomeReportController> logger)
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
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? partyId, string? status)
        {
            ViewData["Title"] = "Income Report";
            ViewData["SubTitle"] = "Warehouse ki aamdani — monthly rent bills aur lifter charges, party-wise.";
            ViewData["ActivePage"] = "IncomeReport";

            int companyId = GetCurrentCompanyId();
            var parsedFrom = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var parsedTo = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            // --- Rent bills in range ---
            var billsQuery = _context.WarehouseRentBills
                .Include(b => b.Party)
                .Where(b => b.CompanyId == companyId
                    && b.BillDate >= parsedFrom && b.BillDate <= parsedTo)
                .AsNoTracking();

            if (partyId.HasValue && partyId.Value > 0)
                billsQuery = billsQuery.Where(b => b.PartyId == partyId.Value);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                billsQuery = billsQuery.Where(b => b.Status == status);

            var bills = await billsQuery.OrderByDescending(b => b.BillDate).ToListAsync();

            // --- Lifter income in range (from billed usages) ---
            var lifterQuery = _context.WarehouseLifterUsages
                .Include(u => u.Party)
                .Where(u => u.CompanyId == companyId
                    && u.UsageDate >= parsedFrom && u.UsageDate <= parsedTo)
                .AsNoTracking();

            if (partyId.HasValue && partyId.Value > 0)
                lifterQuery = lifterQuery.Where(u => u.PartyId == partyId.Value);

            var lifterUsages = await lifterQuery.OrderByDescending(u => u.UsageDate).ToListAsync();

            // --- Party-wise summary ---
            var partySummary = bills
                .GroupBy(b => new { b.PartyId, PartyName = b.Party != null ? b.Party.Name : "N/A" })
                .Select(g => new
                {
                    PartyId = g.Key.PartyId,
                    PartyName = g.Key.PartyName,
                    BillCount = g.Count(),
                    StorageRent = g.Sum(b => b.StorageRentTotal),
                    LifterCharges = g.Sum(b => b.LifterChargesTotal),
                    OtherCharges = g.Sum(b => b.OtherCharges),
                    GrandTotal = g.Sum(b => b.GrandTotal),
                    PaidBills = g.Count(b => b.Status == "Paid"),
                    DueBills = g.Count(b => b.Status != "Paid")
                })
                .OrderByDescending(x => x.GrandTotal)
                .ToList();

            ViewBag.Bills = bills;
            ViewBag.LifterUsages = lifterUsages;
            ViewBag.PartySummary = partySummary;

            ViewBag.TotalStorageRent = bills.Sum(b => b.StorageRentTotal);
            ViewBag.TotalLifter = bills.Sum(b => b.LifterChargesTotal);
            ViewBag.TotalOther = bills.Sum(b => b.OtherCharges);
            ViewBag.GrandTotal = bills.Sum(b => b.GrandTotal);
            ViewBag.BillCount = bills.Count;
            ViewBag.UnbilledLifterTotal = lifterUsages.Where(u => u.BilledStatus == "Unbilled").Sum(u => u.TotalAmount);

            ViewBag.Parties = await _context.Vendors
                .Where(v => (v.Active == true || v.Active == null))
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

            ViewBag.FromDate = parsedFrom.ToString("yyyy-MM-dd");
            ViewBag.ToDate = parsedTo.Date.ToString("yyyy-MM-dd");
            ViewBag.SelectedPartyId = partyId;
            ViewBag.SelectedStatus = status ?? "All";

            return View();
        }
    }
}
