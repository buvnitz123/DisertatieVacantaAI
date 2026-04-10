using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("AppSettings")]
    public class AppSettings
    {
        [Key]
        [Required]
        [StringLength(255)]
        [Column("ParamKey")]
        public string ParamKey { get; set; }

        [Required]
        [Column("ParamValue")]
        public string ParamValue { get; set; }
    }
}
