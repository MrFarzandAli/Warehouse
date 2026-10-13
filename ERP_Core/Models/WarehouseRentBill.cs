using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    [Table("WarehouseRentBills")]
    public class WarehouseRentBill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BillId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Bill No")]
        public string BillNo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bill Date")]
        public DateTime BillDate { get; set; } = DateTime.Today;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Billing Month")]
        public string BillingMonth { get; set; } = DateTime.Today.ToString("yyyy-MM");

        [Required]
        [Display(Name = "Party / Customer")]
        public int PartyId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Storage Rent Total")]
        public decimal StorageRentTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Lifter Charges Total")]
        public decimal LifterChargesTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Other Charges")]
        public decimal OtherCharges { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; } = 0;

        public int? VoucherId { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Due"; // Due, Paid, Partial

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int CompanyId { get; set; } = 1;

        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(PartyId))]
        public virtual Vendor? Party { get; set; }

        public virtual ICollection<WarehouseRentBillDetail> Details { get; set; } = new List<WarehouseRentBillDetail>();
    }
}
