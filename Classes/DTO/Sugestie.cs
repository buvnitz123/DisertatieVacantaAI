using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("Sugestie")]
    public class Sugestie
    {
        [Key]
        [Column("Id_Sugestie")]
        public int Id_Sugestie { get; set; }

        [Required]
        [Column("Data_Inregistrare")]
        public DateTime Data_Inregistrare { get; set; }

        [Column("EsteGenerataDeAI")]
        public int? EsteGenerataDeAI { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Titlu")]
        public string Titlu { get; set; }

        [Required]
        [Column("Buget_Estimat")]
        public decimal Buget_Estimat { get; set; }

        [Required]
        [StringLength(4000)]
        [Column("Descriere")]
        public string Descriere { get; set; }

        [Column("EstePublic")]
        public int? EstePublic { get; set; }

        [StringLength(50)]
        [Column("CodPartajare")]
        public string CodPartajare { get; set; }

        [Required]
        [Column("Id_Destinatie")]
        public int Id_Destinatie { get; set; }

        [Required]
        [Column("Id_Utilizator")]
        public int Id_Utilizator { get; set; }

        [ForeignKey("Id_Destinatie")]
        public virtual Destinatie Destinatie { get; set; }

        [ForeignKey("Id_Utilizator")]
        public virtual Utilizator Utilizator { get; set; }
    }
}
