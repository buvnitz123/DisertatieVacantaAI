using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("Recenzie")]
    public class Recenzie
    {
        [Key]
        [Column("Id_Recenzie")]
        public int Id_Recenzie { get; set; }

        [Required]
        [Column("Nota")]
        public int Nota { get; set; }

        [StringLength(250)]
        [Column("Comentariu")]
        public string Comentariu { get; set; }

        [Required]
        [Column("Data_Creare")]
        public DateTime Data_Creare { get; set; }

        [Required]
        [Column("Id_PunctDeInteres")]
        public int Id_PunctDeInteres { get; set; }

        [Required]
        [Column("Id_Destinatie")]
        public int Id_Destinatie { get; set; }

        [Required]
        [Column("Id_Utilizator")]
        public int Id_Utilizator { get; set; }

        [ForeignKey("Id_PunctDeInteres")]
        public virtual PunctDeInteres PunctDeInteres { get; set; }

        [ForeignKey("Id_Destinatie")]
        public virtual Destinatie Destinatie { get; set; }

        [ForeignKey("Id_Utilizator")]
        public virtual Utilizator Utilizator { get; set; }
    }
}
