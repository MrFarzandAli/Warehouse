using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    [Table("WarehouseLots")]
    public class WarehouseLot : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LotId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Lot No")]
        public string LotNo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Inward Date")]
        public DateTime InwardDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Party / Customer")]
        public int PartyId { get; set; }

        [Required]
        [Display(Name = "Item")]
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Inward Quantity")]
        public decimal OriginalQty { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Current Remaining Quantity")]
        public decimal CurrentRemainingQty { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Monthly Rent Rate")]
        public decimal MonthlyRentRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "First Month Amount")]
        public decimal FirstMonthAmount { get; set; } = 0;

        [Display(Name = "Next Billing Date")]
        public DateTime NextBillingDate { get; set; }

        [Display(Name = "Last Billed Date")]
        public DateTime? LastBilledDate { get; set; }

        [MaxLength(100)]
        [Display(Name = "Bay / Rack Location")]
        public string? BayOrLocation { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active"; // Active, Closed

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Vehicle No")]
        public string? VehicleNo { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int CompanyId { get; set; } = 1;

        // Navigation Properties
        [ForeignKey(nameof(PartyId))]
        public virtual Vendor? Party { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual WarehouseItem? Product { get; set; }

        public virtual ICollection<WarehouseStockOutward> OutwardEntries { get; set; } = new List<WarehouseStockOutward>();
        public virtual ICollection<WarehouseLifterUsage> LifterUsages { get; set; } = new List<WarehouseLifterUsage>();
    }
}
