using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("Favorite")]
    public class Favorite
    {
        [Key]
        [Column("Id_Utilizator", Order = 1)]
        public int Id_Utilizator { get; set; }

        [Key]
        [StringLength(50)]
        [Column("TipElement", Order = 2)]
        public string TipElement { get; set; }

        [Key]
        [Column("Id_Element", Order = 3)]
        public int Id_Element { get; set; }

        [ForeignKey("Id_Utilizator")]
        public virtual Utilizator Utilizator { get; set; }
    }
}
