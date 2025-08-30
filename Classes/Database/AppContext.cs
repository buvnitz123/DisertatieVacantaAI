using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

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