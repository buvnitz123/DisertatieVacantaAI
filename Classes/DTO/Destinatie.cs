using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("Destinatie")]
    public class Destinatie
    {
        [Key]
        [Column("Id_Destinatie")]
        public int Id_Destinatie { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Denumire")]
        public string Denumire { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Tara")]
        public string Tara { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Oras")]
        public string Oras { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Regiune")]
        public string Regiune { get; set; }

        [StringLength(4000)]
        [Column("Descriere")]
        public string Descriere { get; set; }

        [Required]
        [Column("Data_Inregistrare")]
        public DateTime Data_Inregistrare { get; set; }

        [Required]
        [Column("PretAdult")]
        public decimal PretAdult { get; set; }

        [Required]
        [Column("PretMinor")]
        public decimal PretMinor { get; set; }
    }
}
