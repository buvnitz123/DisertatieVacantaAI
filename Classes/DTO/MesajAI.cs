using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("MesajAI")]
    public class MesajAI
    {
        [Key]
        [Column("Id_Mesaj")]
        public int Id_Mesaj { get; set; }

        [Required]
        [StringLength(4000)]
        [Column("Mesaj")]
        public string Mesaj { get; set; }

        [Required]
        [Column("Data_Creare")]
        public DateTime Data_Creare { get; set; }

        [Required]
        [Column("Mesaj_User")]
        public int Mesaj_User { get; set; }

        [Required]
        [Column("Id_ConversatieAI")]
        public int Id_ConversatieAI { get; set; }

        [ForeignKey("Id_ConversatieAI")]
        public virtual ConversatieAI ConversatieAI { get; set; }
    }
}
