using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Core.Models
{
    /// <summary>
    /// Lifter / forklift rate master — peak vs normal time rates per equipment.
    /// Lifter entry form fetches rates from here.
    /// </summary>
    [Table("LifterRates")]
    public class LifterRate : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Equipment name is required.")]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        [Display(Name = "Equipment Name")]
        public string EquipmentName { get; set; } = "Lifter 1";

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Normal Time Rate (per hour)")]
        public decimal NormalRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Peak Time Rate (per hour)")]
        public decimal PeakRate { get; set; } = 0;

        [Display(Name = "Peak Start Time")]
        public TimeSpan? PeakStartTime { get; set; }

        [Display(Name = "Peak End Time")]
        public TimeSpan? PeakEndTime { get; set; }

        [MaxLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }
    }
}
