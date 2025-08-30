using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class UtilizatorRepository : IRepository<Utilizator>
    {
        private readonly AppContext _context;

        public UtilizatorRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<Utilizator> GetAll()
        {
            return _context.Utilizatori.ToList();
        }

        public Utilizator GetById(int id)
        {
            return _context.Utilizatori.Find(id);
        }

        public void Insert(Utilizator entity)
        {
            _context.Utilizatori.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Utilizator entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.Utilizatori.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
