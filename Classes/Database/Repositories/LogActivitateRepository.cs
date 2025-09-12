using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class LogActivitateRepository : IRepository<LogActivitate>
    {
        public IEnumerable<LogActivitate> GetAll()
        {
            using var context = new AppContext();
            return context.LogActivitate.ToList();
        }

        public LogActivitate GetById(int id)
        {
            using var context = new AppContext();
            return context.LogActivitate.Find(id);
        }

        public void Insert(LogActivitate entity)
        {
            using var context = new AppContext();
            context.LogActivitate.Add(entity);
            context.SaveChanges();
        }

        public void Update(LogActivitate entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.LogActivitate.Find(id);
            if (entity != null)
            {
                context.LogActivitate.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
