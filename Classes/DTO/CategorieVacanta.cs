using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("CategorieVacanta")]
    public class CategorieVacanta
    {
        [Key]
        [Column("Id_CategorieVacanta")]
        public int Id_CategorieVacanta { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Denumire")]
        public string Denumire { get; set; }

        [StringLength(100)]
        [Column("ImagineUrl")]
        public string ImagineUrl { get; set; }
    }
}