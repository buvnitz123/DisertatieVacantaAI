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
        private readonly AppContext _context;

        public PreferinteUtilizatorRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<PreferinteUtilizator> GetAll()
        {
            return _context.PreferinteUtilizator.ToList();
        }

        public PreferinteUtilizator GetById(int id)
        {
            return _context.PreferinteUtilizator.Find(id);
        }

        public void Insert(PreferinteUtilizator entity)
        {
            _context.PreferinteUtilizator.Add(entity);
            _context.SaveChanges();
        }

        public void Update(PreferinteUtilizator entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.PreferinteUtilizator.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
