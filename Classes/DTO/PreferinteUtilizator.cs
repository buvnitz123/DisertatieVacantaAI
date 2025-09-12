using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("PreferinteUtilizator")]
    public class PreferinteUtilizator
    {
        [Key]
        [Column("Id_PreferinteUtilizator")]
        public int Id_PreferinteUtilizator { get; set; }

        [Required]
        [Column("Data_Inregistrare")]
        public DateTime Data_Inregistrare { get; set; }

        [Column("Buget_Minim")]
        public decimal? Buget_Minim { get; set; }

        [Column("Buget_Maxim")]
        public decimal? Buget_Maxim { get; set; }

        [Required]
        [Column("Id_CategorieVacanta")]
        public int Id_CategorieVacanta { get; set; }

        [Required]
        [Column("Id_Destinatie")]
        public int Id_Destinatie { get; set; }

        [Required]
        [Column("Id_Utilizator")]
        public int Id_Utilizator { get; set; }

        [ForeignKey("Id_CategorieVacanta")]
        public virtual CategorieVacanta CategorieVacanta { get; set; }

        [ForeignKey("Id_Destinatie")]
        public virtual Destinatie Destinatie { get; set; }

        [ForeignKey("Id_Utilizator")]
        public virtual Utilizator Utilizator { get; set; }
    }
}
