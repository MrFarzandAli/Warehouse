using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ERP_Core.Data;
using ERP_Core.Dtos;
using ERP_Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP_Core.Controllers.Warehouse
{
    [Authorize]
    [Route("Warehouse")]
    public class WarehouseController : Controller
    {
        private readonly ERPDbContext _context;
        private readonly ILogger<WarehouseController> _logger;

        public WarehouseController(ERPDbContext context, ILogger<WarehouseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetCurrentCompanyId()
        {
            return HttpContext.Session.GetInt32("CompanyId") ?? 1;
        }

        private string GetCurrentUserName()
        {
            return User.Identity?.Name ?? "Admin";
        }

        // =========================================================================
        // 1. INWARD / LOTS LISTING & ENTRY
        // =========================================================================
        [HttpGet]
        [Route("")]
        [Route("Inward")]
        [Route("Lots")]
        public async Task<IActionResult> Inward(int? partyId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            ViewData["Title"] = "Inward Lots (Stock In)";
            ViewData["SubTitle"] = "Party-wise maal inward karein, monthly rate aur lot assign karein.";
            ViewData["ActivePage"] = "WarehouseInward";

            int companyId = GetCurrentCompanyId();

            var query = _context.WarehouseLots
                .Include(l => l.Party)
                .Include(l => l.Product)
                .Where(l => l.CompanyId == companyId)
                .AsNoTracking()
                .AsQueryable();

            if (partyId.HasValue && partyId.Value > 0)
                query = query.Where(l => l.PartyId == partyId.Value);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = query.Where(l => l.Status == status);

            if (fromDate.HasValue)
                query = query.Where(l => l.InwardDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(l => l.InwardDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            var lots = await query.OrderByDescending(l => l.LotId).ToListAsync();

            ViewBag.Parties = await _context.Vendors
                .Where(v => v.Active == true || v.Active == null)
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name, v.Phone })
                .ToListAsync();

            ViewBag.Products = await _context.Fishes
                .Where(p => p.Active == true || p.Active == null)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Code, p.Unit })
                .ToListAsync();

            ViewBag.SelectedPartyId = partyId;
            ViewBag.SelectedStatus = status ?? "Active";

            return View(lots);
        }

        [HttpPost]
        [Route("CreateLot")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLot([FromForm] InwardLotCreateDto dto)
        {
            if (dto.PartyId <= 0 || dto.ProductId <= 0 || dto.OriginalQty <= 0 || dto.MonthlyRentRate <= 0)
            {
                TempData["ErrorMessage"] = "Party, Product, Quantity aur Monthly Rate required hain!";
                return RedirectToAction(nameof(Inward));
            }

            try
            {
                int companyId = GetCurrentCompanyId();
                string userName = GetCurrentUserName();

                // Generate Unique Lot No: LOT-yyyyMMdd-0001
                string datePrefix = $"LOT-{dto.InwardDate:yyyyMMdd}-";
                int countToday = await _context.WarehouseLots
                    .CountAsync(l => l.LotNo.StartsWith(datePrefix));
                string lotNo = $"{datePrefix}{(countToday + 1):D4}";

                // Ensure unique
                while (await _context.WarehouseLots.AnyAsync(l => l.LotNo == lotNo))
                {
                    countToday++;
                    lotNo = $"{datePrefix}{(countToday + 1):D4}";
                }

                decimal firstMonthAmount = dto.OriginalQty * dto.MonthlyRentRate;
                DateTime nextBillingDate = dto.InwardDate.AddMonths(1);

                var lot = new WarehouseLot
                {
                    LotNo = lotNo,
                    InwardDate = dto.InwardDate,
                    PartyId = dto.PartyId,
                    ProductId = dto.ProductId,
                    OriginalQty = dto.OriginalQty,
                    CurrentRemainingQty = dto.OriginalQty,
                    MonthlyRentRate = dto.MonthlyRentRate,
                    FirstMonthAmount = firstMonthAmount,
                    NextBillingDate = nextBillingDate,
                    BayOrLocation = dto.BayOrLocation,
                    Status = "Active",
                    Remarks = dto.Remarks,
                    CompanyId = companyId,
                    CreatedBy = userName,
                    CreatedOn = DateTime.Now,
                    Active = true
                };

                await _context.WarehouseLots.AddAsync(lot);
                await _context.SaveChangesAsync();

                // If Lifter used on Inward
                if (dto.UseLifter && dto.LifterQtyOrHours > 0 && dto.LifterRate > 0)
                {
                    var lifter = new WarehouseLifterUsage
                    {
                        UsageDate = dto.InwardDate,
                        PartyId = dto.PartyId,
                        LotId = lot.LotId,
                        UsageType = "Hours",
                        QuantityOrHours = dto.LifterQtyOrHours,
                        Rate = dto.LifterRate,
                        TotalAmount = dto.LifterQtyOrHours * dto.LifterRate,
                        EquipmentName = "Lifter 1",
                        BilledStatus = "Unbilled",
                        Remarks = $"Inward Unloading for Lot #{lotNo}",
                        CompanyId = companyId,
                        CreatedBy = userName,
                        CreatedOn = DateTime.Now
                    };
                    await _context.WarehouseLifterUsages.AddAsync(lifter);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Lot {lotNo} kamyabi se darj ho gayi! First Month Rent: Rs {firstMonthAmount:N0}, Next Billing: {nextBillingDate:dd-MMM-yyyy}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lot");
                TempData["ErrorMessage"] = "Lot save karne mein masla aaya: " + ex.Message;
            }

            return RedirectToAction(nameof(Inward));
        }

        // =========================================================================
        // 2. OUTWARD / DISPATCH (STOCK OUT)
        // =========================================================================
        [HttpGet]
        [Route("Outward")]
        public async Task<IActionResult> Outward(int? partyId, DateTime? fromDate, DateTime? toDate)
        {
            ViewData["Title"] = "Outward (Stock Out / Gate Pass)";
            ViewData["SubTitle"] = "Lot select karke maal out karein — remaining stock auto calculate hoga.";
            ViewData["ActivePage"] = "WarehouseOutward";

            int companyId = GetCurrentCompanyId();

            var query = _context.WarehouseStockOutwards
                .Include(o => o.Lot)
                    .ThenInclude(l => l!.Product)
                .Include(o => o.Party)
                .Where(o => o.CompanyId == companyId)
                .AsNoTracking()
                .AsQueryable();

            if (partyId.HasValue && partyId.Value > 0)
                query = query.Where(o => o.PartyId == partyId.Value);

            if (fromDate.HasValue)
                query = query.Where(o => o.OutwardDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(o => o.OutwardDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            var outwardList = await query.OrderByDescending(o => o.OutwardId).ToListAsync();

            // Load active lots with remaining qty > 0
            ViewBag.ActiveLots = await _context.WarehouseLots
                .Include(l => l.Party)
                .Include(l => l.Product)
                .Where(l => l.CompanyId == companyId && l.CurrentRemainingQty > 0 && l.Status == "Active")
                .OrderByDescending(l => l.LotId)
                .Select(l => new LotDropdownDto
                {
                    LotId = l.LotId,
                    LotNo = l.LotNo,
                    PartyName = l.Party != null ? l.Party.Name : "N/A",
                    ProductName = l.Product != null ? l.Product.Name : "N/A",
                    CurrentRemainingQty = l.CurrentRemainingQty,
                    MonthlyRentRate = l.MonthlyRentRate,
                    InwardDateStr = l.InwardDate.ToString("dd-MMM-yyyy"),
                    NextBillingDateStr = l.NextBillingDate.ToString("dd-MMM-yyyy")
                })
                .ToListAsync();

            ViewBag.Parties = await _context.Vendors
                .Where(v => v.Active == true || v.Active == null)
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

            return View(outwardList);
        }

        [HttpPost]
        [Route("CreateOutward")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOutward([FromForm] StockOutwardCreateDto dto)
        {
            if (dto.LotId <= 0 || dto.OutQty <= 0)
            {
                TempData["ErrorMessage"] = "Lot aur valid Out Quantity select karein!";
                return RedirectToAction(nameof(Outward));
            }

            try
            {
                int companyId = GetCurrentCompanyId();
                string userName = GetCurrentUserName();

                var lot = await _context.WarehouseLots.FindAsync(dto.LotId);
                if (lot == null)
                {
                    TempData["ErrorMessage"] = "Selected Lot record nahi mila!";
                    return RedirectToAction(nameof(Outward));
                }

                if (dto.OutQty > lot.CurrentRemainingQty)
                {
                    TempData["ErrorMessage"] = $"Out quantity ({dto.OutQty}) mojuda remaining stock ({lot.CurrentRemainingQty}) se zyada nahi ho sakti!";
                    return RedirectToAction(nameof(Outward));
                }

                decimal remainingAfter = lot.CurrentRemainingQty - dto.OutQty;
                lot.CurrentRemainingQty = remainingAfter;
                if (remainingAfter <= 0)
                {
                    lot.Status = "Closed";
                }
                lot.ModifiedOn = DateTime.Now;
                lot.ModifiedBy = userName;

                // Generate Outward No
                string datePrefix = $"OUT-{dto.OutwardDate:yyyyMMdd}-";
                int countToday = await _context.WarehouseStockOutwards
                    .CountAsync(o => o.OutwardNo.StartsWith(datePrefix));
                string outwardNo = $"{datePrefix}{(countToday + 1):D4}";

                var outward = new WarehouseStockOutward
                {
                    OutwardNo = outwardNo,
                    OutwardDate = dto.OutwardDate,
                    LotId = dto.LotId,
                    PartyId = lot.PartyId,
                    OutQty = dto.OutQty,
                    RemainingQtyAfter = remainingAfter,
                    GatePassNo = dto.GatePassNo,
                    ReceiverName = dto.ReceiverName,
                    VehicleNo = dto.VehicleNo,
                    Remarks = dto.Remarks,
                    CompanyId = companyId,
                    CreatedBy = userName,
                    CreatedOn = DateTime.Now
                };

                await _context.WarehouseStockOutwards.AddAsync(outward);
                await _context.SaveChangesAsync();

                // If Lifter used on Outward
                if (dto.UseLifter && dto.LifterQtyOrHours > 0 && dto.LifterRate > 0)
                {
                    var lifter = new WarehouseLifterUsage
                    {
                        UsageDate = dto.OutwardDate,
                        PartyId = lot.PartyId,
                        LotId = lot.LotId,
                        OutwardId = outward.OutwardId,
                        UsageType = "Hours",
                        QuantityOrHours = dto.LifterQtyOrHours,
                        Rate = dto.LifterRate,
                        TotalAmount = dto.LifterQtyOrHours * dto.LifterRate,
                        EquipmentName = "Lifter 1",
                        BilledStatus = "Unbilled",
                        Remarks = $"Outward Loading for GatePass #{outwardNo} (Lot #{lot.LotNo})",
                        CompanyId = companyId,
                        CreatedBy = userName,
                        CreatedOn = DateTime.Now
                    };
                    await _context.WarehouseLifterUsages.AddAsync(lifter);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Outward {outwardNo} darj ho gaya! Lot #{lot.LotNo} ka baqaya stock: {remainingAfter}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating outward");
                TempData["ErrorMessage"] = "Outward entry save karne mein masla aaya: " + ex.Message;
            }

            return RedirectToAction(nameof(Outward));
        }

        // =========================================================================
        // 3. LIFTER USAGES / LOG
        // =========================================================================
        [HttpGet]
        [Route("Lifter")]
        public async Task<IActionResult> Lifter(int? partyId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            ViewData["Title"] = "Lifter Usage & Log";
            ViewData["SubTitle"] = "Lifter / Forklift ka rozana istemaal aur party-wise billing track karein.";
            ViewData["ActivePage"] = "WarehouseLifter";

            int companyId = GetCurrentCompanyId();

            var query = _context.WarehouseLifterUsages
                .Include(u => u.Party)
                .Include(u => u.Lot)
                .Where(u => u.CompanyId == companyId)
                .AsNoTracking()
                .AsQueryable();

            if (partyId.HasValue && partyId.Value > 0)
                query = query.Where(u => u.PartyId == partyId.Value);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = query.Where(u => u.BilledStatus == status);

            if (fromDate.HasValue)
                query = query.Where(u => u.UsageDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(u => u.UsageDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            var usages = await query.OrderByDescending(u => u.LifterUsageId).ToListAsync();

            ViewBag.Parties = await _context.Vendors
                .Where(v => v.Active == true || v.Active == null)
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

            ViewBag.ActiveLots = await _context.WarehouseLots
                .Where(l => l.CompanyId == companyId && l.Status == "Active")
                .OrderByDescending(l => l.LotId)
                .Select(l => new { l.LotId, l.LotNo, PartyName = l.Party != null ? l.Party.Name : "" })
                .ToListAsync();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.SelectedPartyId = partyId;

            return View(usages);
        }

        [HttpPost]
        [Route("CreateLifterUsage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLifterUsage([FromForm] LifterUsageCreateDto dto)
        {
            if (dto.PartyId <= 0 || dto.QuantityOrHours <= 0 || dto.Rate <= 0)
            {
                TempData["ErrorMessage"] = "Party, Hours/Trips aur Rate zaroori hain!";
                return RedirectToAction(nameof(Lifter));
            }

            try
            {
                int companyId = GetCurrentCompanyId();
                string userName = GetCurrentUserName();

                var usage = new WarehouseLifterUsage
                {
                    UsageDate = dto.UsageDate,
                    PartyId = dto.PartyId,
                    LotId = dto.LotId > 0 ? dto.LotId : null,
                    UsageType = dto.UsageType,
                    QuantityOrHours = dto.QuantityOrHours,
                    Rate = dto.Rate,
                    TotalAmount = dto.QuantityOrHours * dto.Rate,
                    OperatorName = dto.OperatorName,
                    EquipmentName = string.IsNullOrWhiteSpace(dto.EquipmentName) ? "Lifter 1" : dto.EquipmentName,
                    BilledStatus = "Unbilled",
                    Remarks = dto.Remarks,
                    CompanyId = companyId,
                    CreatedBy = userName,
                    CreatedOn = DateTime.Now
                };

                await _context.WarehouseLifterUsages.AddAsync(usage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Lifter usage save ho gaya! Total Amount: Rs {usage.TotalAmount:N0} (Status: Unbilled).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lifter usage");
                TempData["ErrorMessage"] = "Lifter entry save karne mein masla aaya: " + ex.Message;
            }

            return RedirectToAction(nameof(Lifter));
        }

        // =========================================================================
        // 4. MONTHLY BILLING / ROLLING BILL GENERATOR
        // =========================================================================
        [HttpGet]
        [Route("Billing")]
        public async Task<IActionResult> Billing(int? partyId, DateTime? billDate)
        {
            ViewData["Title"] = "Monthly Rent Invoicing";
            ViewData["SubTitle"] = "Remaining stock par monthly rolling bill aur lifter charges generate karein.";
            ViewData["ActivePage"] = "WarehouseBilling";

            int companyId = GetCurrentCompanyId();
            DateTime targetDate = billDate ?? DateTime.Today;

            // 1. Lots due for billing: NextBillingDate <= targetDate AND CurrentRemainingQty > 0
            var dueLotsQuery = _context.WarehouseLots
                .Include(l => l.Party)
                .Include(l => l.Product)
                .Where(l => l.CompanyId == companyId && l.Status == "Active" && l.CurrentRemainingQty > 0)
                .Where(l => l.NextBillingDate.Date <= targetDate.Date)
                .AsNoTracking();

            if (partyId.HasValue && partyId.Value > 0)
                dueLotsQuery = dueLotsQuery.Where(l => l.PartyId == partyId.Value);

            var dueLots = await dueLotsQuery.OrderBy(l => l.NextBillingDate).ToListAsync();

            // 2. Unbilled Lifter charges
            var unbilledLifterQuery = _context.WarehouseLifterUsages
                .Include(u => u.Party)
                .Where(u => u.CompanyId == companyId && u.BilledStatus == "Unbilled")
                .AsNoTracking();

            if (partyId.HasValue && partyId.Value > 0)
                unbilledLifterQuery = unbilledLifterQuery.Where(u => u.PartyId == partyId.Value);

            var unbilledLifter = await unbilledLifterQuery.OrderBy(u => u.UsageDate).ToListAsync();

            // 3. Existing Generated Bills
            var billsQuery = _context.WarehouseRentBills
                .Include(b => b.Party)
                .Where(b => b.CompanyId == companyId)
                .AsNoTracking();

            if (partyId.HasValue && partyId.Value > 0)
                billsQuery = billsQuery.Where(b => b.PartyId == partyId.Value);

            var bills = await billsQuery.OrderByDescending(b => b.BillId).Take(50).ToListAsync();

            ViewBag.Parties = await _context.Vendors
                .Where(v => v.Active == true || v.Active == null)
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

            ViewBag.DueLots = dueLots;
            ViewBag.UnbilledLifter = unbilledLifter;
            ViewBag.TargetDate = targetDate.ToString("yyyy-MM-dd");
            ViewBag.SelectedPartyId = partyId;

            return View(bills);
        }

        [HttpPost]
        [Route("GenerateBill")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBill([FromForm] GenerateRentBillDto dto)
        {
            if (dto.PartyId <= 0)
            {
                TempData["ErrorMessage"] = "Party select karein jiska bill banana hai!";
                return RedirectToAction(nameof(Billing));
            }

            try
            {
                int companyId = GetCurrentCompanyId();
                string userName = GetCurrentUserName();

                var party = await _context.Vendors.FindAsync(dto.PartyId);
                if (party == null)
                {
                    TempData["ErrorMessage"] = "Party nahi mili!";
                    return RedirectToAction(nameof(Billing));
                }

                // 1. Fetch Selected or Due Lots for this party
                var lotsQuery = _context.WarehouseLots
                    .Where(l => l.CompanyId == companyId && l.PartyId == dto.PartyId && l.Status == "Active" && l.CurrentRemainingQty > 0);

                if (dto.SelectedLotIds != null && dto.SelectedLotIds.Count > 0)
                {
                    lotsQuery = lotsQuery.Where(l => dto.SelectedLotIds.Contains(l.LotId));
                }
                else
                {
                    lotsQuery = lotsQuery.Where(l => l.NextBillingDate.Date <= dto.BillDate.Date);
                }

                var lotsToBill = await lotsQuery.Include(l => l.Product).ToListAsync();

                // 2. Fetch Unbilled Lifter usages
                var lifterQuery = _context.WarehouseLifterUsages
                    .Where(u => u.CompanyId == companyId && u.PartyId == dto.PartyId && u.BilledStatus == "Unbilled");

                if (dto.SelectedLifterUsageIds != null && dto.SelectedLifterUsageIds.Count > 0)
                {
                    lifterQuery = lifterQuery.Where(u => dto.SelectedLifterUsageIds.Contains(u.LifterUsageId));
                }

                var lifterToBill = await lifterQuery.ToListAsync();

                if (!lotsToBill.Any() && !lifterToBill.Any())
                {
                    TempData["ErrorMessage"] = "Is party ke paas koi due lot ya unbilled lifter usage nahi hai!";
                    return RedirectToAction(nameof(Billing));
                }

                // Generate Bill No: RB-yyyyMMdd-0001
                string datePrefix = $"RB-{dto.BillDate:yyyyMMdd}-";
                int countToday = await _context.WarehouseRentBills
                    .CountAsync(b => b.BillNo.StartsWith(datePrefix));
                string billNo = $"{datePrefix}{(countToday + 1):D4}";

                decimal storageTotal = 0;
                decimal lifterTotal = 0;

                var bill = new WarehouseRentBill
                {
                    BillNo = billNo,
                    BillDate = dto.BillDate,
                    BillingMonth = dto.BillingMonth,
                    PartyId = dto.PartyId,
                    Status = "Due",
                    OtherCharges = dto.OtherCharges,
                    Remarks = dto.Remarks,
                    CompanyId = companyId,
                    CreatedBy = userName,
                    CreatedOn = DateTime.Now
                };

                await _context.WarehouseRentBills.AddAsync(bill);
                await _context.SaveChangesAsync(); // save to get bill.BillId

                // Detail lines
                var detailList = new List<WarehouseRentBillDetail>();

                // A. Add Storage Rent Lines
                foreach (var lot in lotsToBill)
                {
                    decimal lotAmount = lot.CurrentRemainingQty * lot.MonthlyRentRate;
                    storageTotal += lotAmount;

                    detailList.Add(new WarehouseRentBillDetail
                    {
                        BillId = bill.BillId,
                        LotId = lot.LotId,
                        ChargeType = "StorageRent",
                        Description = $"Monthly Storage Rent - Lot #{lot.LotNo} ({lot.Product?.Name ?? "Item"}): {lot.CurrentRemainingQty:N0} remaining @ Rs {lot.MonthlyRentRate:N0}",
                        BilledQty = lot.CurrentRemainingQty,
                        Rate = lot.MonthlyRentRate,
                        Amount = lotAmount
                    });

                    // Advance Lot Rolling Billing Date by 1 Month
                    lot.LastBilledDate = dto.BillDate;
                    lot.NextBillingDate = lot.NextBillingDate.AddMonths(1);
                    lot.ModifiedOn = DateTime.Now;
                    lot.ModifiedBy = userName;
                }

                // B. Add Lifter Lines
                foreach (var lft in lifterToBill)
                {
                    lifterTotal += lft.TotalAmount;

                    detailList.Add(new WarehouseRentBillDetail
                    {
                        BillId = bill.BillId,
                        LotId = lft.LotId,
                        ChargeType = "LifterRent",
                        Description = $"Lifter Charges - {lft.UsageType} on {lft.UsageDate:dd-MMM} ({lft.QuantityOrHours:N0} @ Rs {lft.Rate:N0})",
                        BilledQty = lft.QuantityOrHours,
                        Rate = lft.Rate,
                        Amount = lft.TotalAmount
                    });

                    lft.BilledStatus = "Billed";
                    lft.BillId = bill.BillId;
                }

                await _context.WarehouseRentBillDetails.AddRangeAsync(detailList);

                // Update totals
                bill.StorageRentTotal = storageTotal;
                bill.LifterChargesTotal = lifterTotal;
                bill.GrandTotal = storageTotal + lifterTotal + dto.OtherCharges;

                await _context.SaveChangesAsync();

                // C. Auto Post to Accounting Ledger (Voucher / AccountTransaction)
                await PostRentBillToAccountsAsync(bill, party, userName, companyId);

                TempData["SuccessMessage"] = $"Monthly Bill {billNo} kamyabi se generate ho gaya! Total Storage: Rs {storageTotal:N0}, Lifter: Rs {lifterTotal:N0}, Grand Total: Rs {bill.GrandTotal:N0}. Next billing date roll ho gayi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating rent bill");
                TempData["ErrorMessage"] = "Bill generate karne mein masla aaya: " + ex.Message;
            }

            return RedirectToAction(nameof(Billing));
        }

        private async Task PostRentBillToAccountsAsync(WarehouseRentBill bill, Vendor party, string userName, int companyId)
        {
            try
            {
                // Find Party Chart of Account
                int partyAccountId = party.ChartOfAccountId ?? 0;
                if (partyAccountId <= 0)
                {
                    // Check if there is an AccountSetup for customer
                    var setup = await _context.AccountSetups.FirstOrDefaultAsync(s => s.CompanyId == companyId);
                    partyAccountId = setup?.CustomerAccID ?? 0;
                }

                // Find or use Revenue Account
                var rentIncomeAccount = await _context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => (a.AccountName.Contains("Rent") || a.AccountName.Contains("Sale") || a.AccountName.Contains("Income")) && a.CompanyId == companyId);

                // If party account exists, create JV or Invoice voucher
                var voucher = new Voucher
                {
                    VoucherNo = bill.BillNo,
                    VoucherDate = bill.BillDate,
                    VoucherType = "JV",
                    Amount = bill.GrandTotal,
                    AmountInWords = $"Rupees {bill.GrandTotal:N2}",
                    VendorId = party.Id,
                    InvoiceNo = bill.BillNo,
                    Narration = $"Monthly Warehouse Rent & Lifter Charges for {party.Name} ({bill.BillingMonth})",
                    ApprovalStatus = "Approved",
                    CompanyId = companyId,
                    CreatedBy = userName,
                    CreatedOn = DateTime.Now
                };

                await _context.Vouchers.AddAsync(voucher);
                await _context.SaveChangesAsync();

                bill.VoucherId = voucher.VoucherId;

                var transactions = new List<AccountTransaction>();

                if (partyAccountId > 0)
                {
                    // Debit Party (Receivable increases)
                    transactions.Add(new AccountTransaction
                    {
                        VoucherId = voucher.VoucherId,
                        VoucherNo = voucher.VoucherNo,
                        ChartofAccountId = partyAccountId,
                        Debit = bill.GrandTotal,
                        Credit = 0,
                        Amount = bill.GrandTotal,
                        Type = "Debit",
                        EntryTypeName = "Warehouse Rent",
                        Narration = $"Rent & Lifter Bill #{bill.BillNo} ({bill.BillingMonth})",
                        CompanyId = companyId
                    });
                }

                if (rentIncomeAccount != null)
                {
                    // Credit Rent Income
                    transactions.Add(new AccountTransaction
                    {
                        VoucherId = voucher.VoucherId,
                        VoucherNo = voucher.VoucherNo,
                        ChartofAccountId = rentIncomeAccount.Id,
                        Debit = 0,
                        Credit = bill.GrandTotal,
                        Amount = bill.GrandTotal,
                        Type = "Credit",
                        EntryTypeName = "Warehouse Rent Revenue",
                        Narration = $"Warehouse Rent Revenue from {party.Name} (#{bill.BillNo})",
                        CompanyId = companyId
                    });
                }

                if (transactions.Any())
                {
                    await _context.AccountTransactions.AddRangeAsync(transactions);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not auto-post rent bill to ledger: {Message}", ex.Message);
            }
        }

        [HttpGet]
        [Route("PrintBill/{id}")]
        public async Task<IActionResult> PrintBill(int id)
        {
            var bill = await _context.WarehouseRentBills
                .Include(b => b.Party)
                .Include(b => b.Details)
                    .ThenInclude(d => d.Lot)
                        .ThenInclude(l => l!.Product)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null) return NotFound("Bill not found");

            var company = await _context.CCompanies.FirstOrDefaultAsync(c => c.CompanyId == bill.CompanyId);
            ViewBag.Company = company;

            return View(bill);
        }

        // =========================================================================
        // 5. STOCK POSITION REPORT
        // =========================================================================
        [HttpGet]
        [Route("StockReport")]
        public async Task<IActionResult> StockReport(int? partyId, int? productId, string? status)
        {
            ViewData["Title"] = "Warehouse Stock Position Report";
            ViewData["SubTitle"] = "Lot-wise mojuda maal, in/out summary aur agle rent ki tareekh.";
            ViewData["ActivePage"] = "WarehouseStockReport";

            int companyId = GetCurrentCompanyId();

            var query = _context.WarehouseLots
                .Include(l => l.Party)
                .Include(l => l.Product)
                .Include(l => l.OutwardEntries)
                .Where(l => l.CompanyId == companyId)
                .AsNoTracking()
                .AsQueryable();

            if (partyId.HasValue && partyId.Value > 0)
                query = query.Where(l => l.PartyId == partyId.Value);

            if (productId.HasValue && productId.Value > 0)
                query = query.Where(l => l.ProductId == productId.Value);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = query.Where(l => l.Status == status);
            else if (string.IsNullOrWhiteSpace(status))
                query = query.Where(l => l.Status == "Active");

            var lots = await query.OrderByDescending(l => l.LotId).ToListAsync();

            ViewBag.Parties = await _context.Vendors
                .Where(v => v.Active == true || v.Active == null)
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

            ViewBag.Products = await _context.Fishes
                .Where(p => p.Active == true || p.Active == null)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.SelectedPartyId = partyId;
            ViewBag.SelectedProductId = productId;
            ViewBag.SelectedStatus = status ?? "Active";

            return View(lots);
        }

        // =========================================================================
        // AJAX API: GET LOT DETAILS FOR OUTWARD
        // =========================================================================
        [HttpGet]
        [Route("GetLotDetails/{id}")]
        public async Task<IActionResult> GetLotDetails(int id)
        {
            var lot = await _context.WarehouseLots
                .Include(l => l.Party)
                .Include(l => l.Product)
                .FirstOrDefaultAsync(l => l.LotId == id);

            if (lot == null) return NotFound();

            return Json(new
            {
                lotId = lot.LotId,
                lotNo = lot.LotNo,
                partyId = lot.PartyId,
                partyName = lot.Party?.Name ?? "N/A",
                productId = lot.ProductId,
                productName = lot.Product?.Name ?? "N/A",
                originalQty = lot.OriginalQty,
                currentRemainingQty = lot.CurrentRemainingQty,
                monthlyRentRate = lot.MonthlyRentRate,
                inwardDate = lot.InwardDate.ToString("dd-MMM-yyyy"),
                nextBillingDate = lot.NextBillingDate.ToString("dd-MMM-yyyy"),
                bayOrLocation = lot.BayOrLocation ?? "N/A"
            });
        }
    }
}
