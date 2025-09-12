using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

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
