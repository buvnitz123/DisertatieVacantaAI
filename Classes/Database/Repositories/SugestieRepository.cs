using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class SugestieRepository : IRepository<Sugestie>
    {
        private readonly AppContext _context;

        public SugestieRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<Sugestie> GetAll()
        {
            return _context.Sugestii.ToList();
        }

        public Sugestie GetById(int id)
        {
            return _context.Sugestii.Find(id);
        }

        public void Insert(Sugestie entity)
        {
            _context.Sugestii.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Sugestie entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.Sugestii.Remove(entity);
                _context.SaveChanges();
            }
        }

        // New: get suggestions for a specific user including destination and ordered by date desc
        public IEnumerable<Sugestie> GetByUser(int userId)
        {
            return _context.Sugestii
                .Include(s => s.Destinatie)
                .Where(s => s.Id_Utilizator == userId)
                .OrderByDescending(s => s.Data_Inregistrare)
                .ToList();
        }
    }
}
