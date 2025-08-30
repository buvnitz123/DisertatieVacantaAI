using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class PunctDeInteresRepository : IRepository<PunctDeInteres>
    {
        private readonly AppContext _context;

        public PunctDeInteresRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<PunctDeInteres> GetAll()
        {
            return _context.PuncteDeInteres.ToList();
        }

        public PunctDeInteres GetById(int id)
        {
            return _context.PuncteDeInteres.Find(id);
        }

        public void Insert(PunctDeInteres entity)
        {
            _context.PuncteDeInteres.Add(entity);
            _context.SaveChanges();
        }

        public void Update(PunctDeInteres entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.PuncteDeInteres.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
