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
        private readonly AppContext _context;

        public LogActivitateRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<LogActivitate> GetAll()
        {
            return _context.LogActivitate.ToList();
        }

        public LogActivitate GetById(int id)
        {
            return _context.LogActivitate.Find(id);
        }

        public void Insert(LogActivitate entity)
        {
            _context.LogActivitate.Add(entity);
            _context.SaveChanges();
        }

        public void Update(LogActivitate entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.LogActivitate.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
