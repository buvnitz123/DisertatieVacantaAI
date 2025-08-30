using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("ImaginiDestinatie")]
    public class ImaginiDestinatie
    {
        [Key]
        [Column("Id_Destinatie", Order = 1)]
        public int Id_Destinatie { get; set; }

        [Key]
        [Column("Id_ImaginiDestinatie", Order = 2)]
        public int Id_ImaginiDestinatie { get; set; }

        [StringLength(200)]
        [Column("ImagineUrl")]
        public string ImagineUrl { get; set; }

        [ForeignKey("Id_Destinatie")]
        public virtual Destinatie Destinatie { get; set; }
    }
}
