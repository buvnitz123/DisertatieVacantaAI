using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MauiAppDisertatieVacantaAI.Classes.DTO
{
    [Table("DestinatieFacilitate")]
    public class DestinatieFacilitate
    {
        [Key]
        [Column("Id_Destinatie", Order = 1)]
        public int Id_Destinatie { get; set; }

        [Key]
        [Column("Id_Facilitate", Order = 2)]
        public int Id_Facilitate { get; set; }

        [ForeignKey("Id_Destinatie")]
        public virtual Destinatie Destinatie { get; set; }

        [ForeignKey("Id_Facilitate")]
        public virtual Facilitate Facilitate { get; set; }
    }
}
