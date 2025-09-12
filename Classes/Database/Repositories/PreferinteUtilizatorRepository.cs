using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class PreferinteUtilizatorRepository : IRepository<PreferinteUtilizator>
    {
        public IEnumerable<PreferinteUtilizator> GetAll()
        {
            using var context = new AppContext();
            return context.PreferinteUtilizator.ToList();
        }

        public PreferinteUtilizator GetById(int id)
        {
            using var context = new AppContext();
            return context.PreferinteUtilizator.Find(id);
        }

        public void Insert(PreferinteUtilizator entity)
        {
            using var context = new AppContext();
            context.PreferinteUtilizator.Add(entity);
            context.SaveChanges();
        }

        public void Update(PreferinteUtilizator entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.PreferinteUtilizator.Find(id);
            if (entity != null)
            {
                context.PreferinteUtilizator.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
