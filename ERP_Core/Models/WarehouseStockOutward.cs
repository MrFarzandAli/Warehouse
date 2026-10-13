using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    [Table("WarehouseStockOutwards")]
    public class WarehouseStockOutward
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OutwardId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Outward No")]
        public string OutwardNo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Outward Date")]
        public DateTime OutwardDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Lot")]
        public int LotId { get; set; }

        [Required]
        [Display(Name = "Party / Customer")]
        public int PartyId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Out Quantity")]
        public decimal OutQty { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Remaining Quantity After Out")]
        public decimal RemainingQtyAfter { get; set; } = 0;

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Gate Pass No")]
        public string? GatePassNo { get; set; }

        [MaxLength(100)]
        [Display(Name = "Receiver Name")]
        public string? ReceiverName { get; set; }

        [MaxLength(50)]
        [Display(Name = "Vehicle No")]
        public string? VehicleNo { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int CompanyId { get; set; } = 1;

        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(LotId))]
        public virtual WarehouseLot? Lot { get; set; }

        [ForeignKey(nameof(PartyId))]
        public virtual Vendor? Party { get; set; }
    }
}
