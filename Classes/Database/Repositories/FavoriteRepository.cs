using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class FavoriteRepository : IRepository<Favorite>
    {
        private readonly AppContext _context;

        public FavoriteRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<Favorite> GetAll()
        {
            return _context.Favorite.ToList();
        }

        public Favorite GetById(int id)
        {
            // Favorite are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public void Insert(Favorite entity)
        {
            _context.Favorite.Add(entity);
            _context.SaveChanges();
        }

        public void Update(Favorite entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            // Favorite are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
