using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    /// <summary>
    /// Generic warehouse item / product master.
    /// Replaces the fish-specific Fish master for warehouse flows.
    /// </summary>
    [Table("WarehouseItems")]
    public class WarehouseItem : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Item Code")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Item Name is required.")]
        [MaxLength(150)]
        [Column(TypeName = "nvarchar(150)")]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Unit")]
        public string Unit { get; set; } = "Bag";

        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [MaxLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }
    }
}
