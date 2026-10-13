using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    [Table("WarehouseLifterUsages")]
    public class WarehouseLifterUsage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LifterUsageId { get; set; }

        [Required]
        [Display(Name = "Usage Date")]
        public DateTime UsageDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Party / Customer")]
        public int PartyId { get; set; }

        [Display(Name = "Associated Lot (Optional)")]
        public int? LotId { get; set; }

        public int? OutwardId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Usage Type")]
        public string UsageType { get; set; } = "Hours"; // Hours, Trips, Lifting

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Qty / Hours")]
        public decimal QuantityOrHours { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Rate")]
        public decimal Rate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; } = 0;

        [MaxLength(100)]
        [Display(Name = "Operator Name")]
        public string? OperatorName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Equipment Name")]
        public string EquipmentName { get; set; } = "Lifter 1";

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Billed Status")]
        public string BilledStatus { get; set; } = "Unbilled"; // Unbilled, Billed

        public int? BillId { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int CompanyId { get; set; } = 1;

        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(PartyId))]
        public virtual Vendor? Party { get; set; }

        [ForeignKey(nameof(LotId))]
        public virtual WarehouseLot? Lot { get; set; }
    }
}
