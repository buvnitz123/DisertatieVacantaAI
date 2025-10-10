using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class PunctDeInteresRepository : IRepository<PunctDeInteres>
    {
        public IEnumerable<PunctDeInteres> GetAll()
        {
            using var context = new AppContext();
            return context.PuncteDeInteres.ToList();
        }

        public PunctDeInteres GetById(int id)
        {
            using var context = new AppContext();
            return context.PuncteDeInteres.Find(id);
        }

        public IEnumerable<PunctDeInteres> GetByDestinationId(int destinationId)
        {
            using var context = new AppContext();
            return context.PuncteDeInteres
                         .Where(p => p.Id_Destinatie == destinationId)
                         .ToList();
        }

        public void Insert(PunctDeInteres entity)
        {
            using var context = new AppContext();
            
            // Generate next ID manually (MAX + 1)
            var maxId = context.PuncteDeInteres.Any() ? context.PuncteDeInteres.Max(p => p.Id_PunctDeInteres) : 0;
            entity.Id_PunctDeInteres = maxId + 1;
            
            System.Diagnostics.Debug.WriteLine($"Generated next PunctDeInteres ID: {entity.Id_PunctDeInteres} (max was: {maxId})");
            
            context.PuncteDeInteres.Add(entity);
            context.SaveChanges();
        }

        public void Update(PunctDeInteres entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.PuncteDeInteres.Find(id);
            if (entity != null)
            {
                context.PuncteDeInteres.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
