using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class DestinatieRepository : IRepository<Destinatie>
    {
        public IEnumerable<Destinatie> GetAll()
        {
            using var context = new AppContext();
            return context.Destinatii.ToList();
        }

        public Destinatie GetById(int id)
        {
            using var context = new AppContext();
            return context.Destinatii.Find(id);
        }

        public void Insert(Destinatie entity)
        {
            using var context = new AppContext();
            
            // Generate next ID manually (MAX + 1)
            var maxId = context.Destinatii.Any() ? context.Destinatii.Max(d => d.Id_Destinatie) : 0;
            entity.Id_Destinatie = maxId + 1;
            
            System.Diagnostics.Debug.WriteLine($"Generated next Destinatie ID: {entity.Id_Destinatie} (max was: {maxId})");
            
            context.Destinatii.Add(entity);
            context.SaveChanges();
        }

        public void Update(Destinatie entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.Destinatii.Find(id);
            if (entity != null)
            {
                context.Destinatii.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
