using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("PunctDeInteres")]
    public class PunctDeInteres
    {
        [Key]
        [Column("Id_PunctDeInteres")]
        public int Id_PunctDeInteres { get; set; }

        [StringLength(50)]
        [Column("Denumire")]
        public string Denumire { get; set; }

        [StringLength(4000)]
        [Column("Descriere")]
        public string Descriere { get; set; }

        [StringLength(50)]
        [Column("Tip")]
        public string Tip { get; set; }

        [Required]
        [Column("Id_Destinatie")]
        public int Id_Destinatie { get; set; }

        [ForeignKey("Id_Destinatie")]
        public virtual Destinatie Destinatie { get; set; }
    }
}
