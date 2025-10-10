using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class FacilitateRepository : IRepository<Facilitate>
    {
        public IEnumerable<Facilitate> GetAll()
        {
            using var context = new AppContext();
            return context.Facilitati.ToList();
        }

        public Facilitate GetById(int id)
        {
            using var context = new AppContext();
            return context.Facilitati.Find(id);
        }

        public void Insert(Facilitate entity)
        {
            using var context = new AppContext();
            
            // Generate next ID manually (MAX + 1)
            var maxId = context.Facilitati.Any() ? context.Facilitati.Max(f => f.Id_Facilitate) : 0;
            entity.Id_Facilitate = maxId + 1;
            
            System.Diagnostics.Debug.WriteLine($"Generated next Facilitate ID: {entity.Id_Facilitate} (max was: {maxId})");
            
            context.Facilitati.Add(entity);
            context.SaveChanges();
        }

        public void Update(Facilitate entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.Facilitati.Find(id);
            if (entity != null)
            {
                context.Facilitati.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
