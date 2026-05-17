using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace MauiAppDisertatieVacantaAI.Classes.Database
{
    public class AppContext : DbContext
    {
        public AppContext() : base(EncryptionUtils.GetDecryptedConnectionString("DbContext")) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CategorieVacanta>().Property(c => c.Id_CategorieVacanta).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<Facilitate>().Property(f => f.Id_Facilitate).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<ConversatieAI>().Property(c => c.Id_ConversatieAI).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<MesajAI>().Property(m => m.Id_Mesaj).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<Sugestie>().Property(s => s.Id_Sugestie).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<Recenzie>().Property(r => r.Id_Recenzie).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<Destinatie>().Property(d => d.Id_Destinatie).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<PunctDeInteres>().Property(p => p.Id_PunctDeInteres).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<AppSettings>().Property(a => a.ParamKey).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<LogActivitate>().Property(l => l.Id_LogActivitate).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<ModelPerformanta>().Property(m => m.Id_ModelPerformanta).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<Favorite>().HasKey(f => new { f.Id_Utilizator, f.TipElement, f.Id_Element });
            modelBuilder.Entity<CategorieVacanta_Destinatie>().HasKey(cd => new { cd.Id_Destinatie, cd.Id_CategorieVacanta });
            modelBuilder.Entity<DestinatieFacilitate>().HasKey(df => new { df.Id_Destinatie, df.Id_Facilitate });
            modelBuilder.Entity<ImaginiDestinatie>().HasKey(id => new { id.Id_Destinatie, id.Id_ImaginiDestinatie });
            modelBuilder.Entity<ImaginiPunctDeInteres>().HasKey(ip => new { ip.Id_PunctDeInteres, ip.Id_ImaginiPunctDeInteres });
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Utilizator> Utilizatori { get; set; }
        public DbSet<CategorieVacanta> CategoriiVacanta { get; set; }
        public DbSet<Destinatie> Destinatii { get; set; }
        public DbSet<Facilitate> Facilitati { get; set; }
        public DbSet<PunctDeInteres> PuncteDeInteres { get; set; }
        public DbSet<Sugestie> Sugestii { get; set; }
        public DbSet<Recenzie> Recenzii { get; set; }
        public DbSet<LogActivitate> LogActivitate { get; set; }
        public DbSet<MesajAI> MesajeAI { get; set; }
        public DbSet<Favorite> Favorite { get; set; }
        public DbSet<ImaginiDestinatie> ImaginiDestinatie { get; set; }
        public DbSet<ImaginiPunctDeInteres> ImaginiPunctDeInteres { get; set; }
        public DbSet<ConversatieAI> ConversatiiAI { get; set; }
        public DbSet<DestinatieFacilitate> DestinatieFacilitate { get; set; }
        public DbSet<CategorieVacanta_Destinatie> CategorieVacanta_Destinatie { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<ModelPerformanta> ModelPerformanta { get; set; }
    }
}