using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("ModelPerformanta")]
    public class ModelPerformanta
    {
        [Key]
        [Column("Id_ModelPerformanta")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id_ModelPerformanta { get; set; }

        [Required]
        [Column("Data_Inregistrare")]
        public DateTime Data_Inregistrare { get; set; }

        [Required]
        [StringLength(50)]
        [Column("NumeModel")]
        public string NumeModel { get; set; }

        [Required]
        [Column("SecundeDurate")]
        public decimal SecundeDurate { get; set; }

        [Required]
        [Column("TokenInput")]
        public int TokenInput { get; set; }

        [Required]
        [Column("TokenOutput")]
        public int TokenOutput { get; set; }

        [Column("ApreciereUser")]
        public int? ApreciereUser { get; set; }
    }
}
