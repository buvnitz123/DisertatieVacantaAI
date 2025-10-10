using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library;
using System.Data.Entity;

namespace MauiAppDisertatieVacantaAI.Classes.Database
{
    public class AppContext : DbContext
    {
        public AppContext() : base(EncryptionUtils.GetDecryptedConnectionString("DbContext")) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CategorieVacanta>()
                .Property(c => c.Id_CategorieVacanta)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            

            modelBuilder.Entity<Facilitate>()
                .Property(f => f.Id_Facilitate)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Configure ConversatieAI to NOT use identity for primary key (manual assignment)
            modelBuilder.Entity<ConversatieAI>()
                .Property(c => c.Id_ConversatieAI)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Configure MesajAI to NOT use identity for primary key (manual assignment)
            modelBuilder.Entity<MesajAI>()
                .Property(m => m.Id_Mesaj)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Configure Sugestie to NOT use identity for primary key (manual assignment)
            modelBuilder.Entity<Sugestie>()
                .Property(s => s.Id_Sugestie)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            
            // Configure Recenzie to NOT use identity for primary key (manual assignment)
            modelBuilder.Entity<Recenzie>()
                .Property(r => r.Id_Recenzie)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Configure Destinatie to NOT use identity - we'll generate manually
            modelBuilder.Entity<Destinatie>()
                .Property(d => d.Id_Destinatie)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Configure PunctDeInteres to NOT use identity - we'll generate manually
            modelBuilder.Entity<PunctDeInteres>()
                .Property(p => p.Id_PunctDeInteres)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Configure Favorite composite key explicitly
            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.Id_Utilizator, f.TipElement, f.Id_Element });

            // Configure CategorieVacanta_Destinatie composite key
            modelBuilder.Entity<CategorieVacanta_Destinatie>()
                .HasKey(cd => new { cd.Id_Destinatie, cd.Id_CategorieVacanta });

            // Configure DestinatieFacilitate composite key
            modelBuilder.Entity<DestinatieFacilitate>()
                .HasKey(df => new { df.Id_Destinatie, df.Id_Facilitate });

            // Configure ImaginiDestinatie composite key
            modelBuilder.Entity<ImaginiDestinatie>()
                .HasKey(id => new { id.Id_Destinatie, id.Id_ImaginiDestinatie });

            // Configure ImaginiPunctDeInteres composite key
            modelBuilder.Entity<ImaginiPunctDeInteres>()
                .HasKey(ip => new { ip.Id_PunctDeInteres, ip.Id_ImaginiPunctDeInteres });
            
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Utilizator> Utilizatori { get; set; }
        public DbSet<CategorieVacanta> CategoriiVacanta { get; set; }
        public DbSet<Destinatie> Destinatii { get; set; }
        public DbSet<Facilitate> Facilitati { get; set; }
        public DbSet<PunctDeInteres> PuncteDeInteres { get; set; }
        public DbSet<Sugestie> Sugestii { get; set; }
        public DbSet<PreferinteUtilizator> PreferinteUtilizator { get; set; }
        public DbSet<Recenzie> Recenzii { get; set; }
        public DbSet<LogActivitate> LogActivitate { get; set; }
        public DbSet<MesajAI> MesajeAI { get; set; }
        public DbSet<Favorite> Favorite { get; set; }
        public DbSet<ImaginiDestinatie> ImaginiDestinatie { get; set; }
        public DbSet<ImaginiPunctDeInteres> ImaginiPunctDeInteres { get; set; }
        public DbSet<ConversatieAI> ConversatiiAI { get; set; }
        public DbSet<DestinatieFacilitate> DestinatieFacilitate { get; set; }
        public DbSet<CategorieVacanta_Destinatie> CategorieVacanta_Destinatie { get; set; }
    }
}