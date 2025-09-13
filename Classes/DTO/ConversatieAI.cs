using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("ConversatieAI")]
    public class ConversatieAI
    {
        [Key]
        [Column("Id_ConversatieAI")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id_ConversatieAI { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Denumire")]
        public string Denumire { get; set; }

        [Required]
        [Column("Data_Creare", TypeName = "datetime2")]
        public DateTime Data_Creare { get; set; }

        [Required]
        [Column("Id_Utilizator")]
        public int Id_Utilizator { get; set; }

        [ForeignKey("Id_Utilizator")]
        public virtual Utilizator Utilizator { get; set; }
    }
}
