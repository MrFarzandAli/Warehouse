using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    [Table("WarehouseRentBillDetails")]
    public class WarehouseRentBillDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DetailId { get; set; }

        [Required]
        public int BillId { get; set; }

        public int? LotId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Charge Type")]
        public string ChargeType { get; set; } = "StorageRent"; // StorageRent, LifterRent, Handling

        [Required]
        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Billed Quantity / Hours")]
        public decimal BilledQty { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        // Navigation Properties
        [ForeignKey(nameof(BillId))]
        public virtual WarehouseRentBill? Bill { get; set; }

        [ForeignKey(nameof(LotId))]
        public virtual WarehouseLot? Lot { get; set; }
    }
}
