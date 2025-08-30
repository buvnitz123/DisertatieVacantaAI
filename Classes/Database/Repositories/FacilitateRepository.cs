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
        private readonly AppContext _context;

        public FacilitateRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<Facilitate> GetAll()
        {
            return _context.Facilitati.ToList();
        }

        public Facilitate GetById(int id)
        {
            return _context.Facilitati.Find(id);
        }

        public void Insert(Facilitate entity)
        {
            _context.Facilitati.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Facilitate entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.Facilitati.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
