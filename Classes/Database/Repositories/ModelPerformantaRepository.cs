using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class ModelPerformantaRepository : IRepository<ModelPerformanta>
    {
        public IEnumerable<ModelPerformanta> GetAll()
        {
            using var context = new AppContext();
            return context.ModelPerformanta.ToList();
        }

        public ModelPerformanta GetById(int id)
        {
            using var context = new AppContext();
            return context.ModelPerformanta.Find(id);
        }

        public void Insert(ModelPerformanta entity)
        {
            using var context = new AppContext();
            context.ModelPerformanta.Add(entity);
            context.SaveChanges();
        }

        public void Update(ModelPerformanta entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.ModelPerformanta.Find(id);
            if (entity != null)
            {
                context.ModelPerformanta.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
