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
