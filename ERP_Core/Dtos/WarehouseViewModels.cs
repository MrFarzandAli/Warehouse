using System;
using System.Collections.Generic;
using ERP_Core.Models;

namespace ERP_Core.Dtos
{
    public class InwardLotCreateDto
    {
        public DateTime InwardDate { get; set; } = DateTime.Today;
        public int PartyId { get; set; }
        public int ProductId { get; set; }
        public decimal OriginalQty { get; set; }
        public decimal MonthlyRentRate { get; set; }
        public string? BayOrLocation { get; set; }
        public string? Remarks { get; set; }

        // Optional Lifter usage on Inward
        public bool UseLifter { get; set; } = false;
        public decimal LifterQtyOrHours { get; set; } = 1;
        public decimal LifterRate { get; set; } = 0;
        public string? VehicleNo { get; set; }
    }

    public class StockOutwardCreateDto
    {
        public DateTime OutwardDate { get; set; } = DateTime.Today;
        public int LotId { get; set; }
        public decimal OutQty { get; set; }
        public string? GatePassNo { get; set; }
        public string? ReceiverName { get; set; }
        public string? VehicleNo { get; set; }
        public string? Remarks { get; set; }

        // Optional Lifter usage on Outward
        public bool UseLifter { get; set; } = false;
        public decimal LifterQtyOrHours { get; set; } = 1;
        public decimal LifterRate { get; set; } = 0;
    }

    public class LifterUsageCreateDto
    {
        public DateTime UsageDate { get; set; } = DateTime.Today;
        public int PartyId { get; set; }
        public int? LotId { get; set; }
        public string UsageType { get; set; } = "Hours"; // Hours, Trips
        public decimal QuantityOrHours { get; set; } = 1;
        public decimal Rate { get; set; } = 0;
        public string? OperatorName { get; set; }
        public string EquipmentName { get; set; } = "Lifter 1";
        public string? Remarks { get; set; }
    }

    public class GenerateRentBillDto
    {
        public int PartyId { get; set; }
        public DateTime BillDate { get; set; } = DateTime.Today;
        public string BillingMonth { get; set; } = DateTime.Today.ToString("yyyy-MM");
        public List<int>? SelectedLotIds { get; set; }
        public List<int>? SelectedLifterUsageIds { get; set; }
        public decimal OtherCharges { get; set; } = 0;
        public string? Remarks { get; set; }
    }

    public class LotDropdownDto
    {
        public int LotId { get; set; }
        public string LotNo { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal CurrentRemainingQty { get; set; }
        public decimal MonthlyRentRate { get; set; }
        public string InwardDateStr { get; set; } = string.Empty;
        public string NextBillingDateStr { get; set; } = string.Empty;
    }
}
