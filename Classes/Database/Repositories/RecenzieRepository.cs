using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class RecenzieRepository : IRepository<Recenzie>
    {
        private readonly AppContext _context;

        public RecenzieRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<Recenzie> GetAll()
        {
            return _context.Recenzii.ToList();
        }

        public Recenzie GetById(int id)
        {
            return _context.Recenzii.Find(id);
        }

        public void Insert(Recenzie entity)
        {
            _context.Recenzii.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Recenzie entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.Recenzii.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
