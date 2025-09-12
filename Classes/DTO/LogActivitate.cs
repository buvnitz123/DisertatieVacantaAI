using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("LogActivitate")]
    public class LogActivitate
    {
        [Key]
        [Column("Id_LogActivitate")]
        public int Id_LogActivitate { get; set; }

        [StringLength(50)]
        [Column("TipEntitate")]
        public string TipEntitate { get; set; }

        [Column("IdEntitate")]
        public int? IdEntitate { get; set; }

        [StringLength(50)]
        [Column("TipActivitate")]
        public string TipActivitate { get; set; }

        [StringLength(50)]
        [Column("IdActivitate")]
        public string IdActivitate { get; set; }

        [Column("DataInregistrare")]
        public DateTime? DataInregistrare { get; set; }

        [Required]
        [Column("Id_Utilizator")]
        public int Id_Utilizator { get; set; }

        [ForeignKey("Id_Utilizator")]
        public virtual Utilizator Utilizator { get; set; }
    }
}
