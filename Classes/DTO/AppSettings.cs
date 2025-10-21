using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        //add max length as NVARCHAR(MAX)
        [Column("ParamValue")]
        public string ParamValue { get; set; }
    }
}
