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

        public IEnumerable<Utilizator> GetAll() => _context.Utilizatori.ToList();

        public Utilizator GetById(int id) => _context.Utilizatori.Find(id);

        public Utilizator GetByEmail(string email) => _context.Utilizatori.FirstOrDefault(u => u.Email == email);

        public bool EmailExists(string email) => _context.Utilizatori.Any(u => u.Email == email);

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

        // Time-based manual ID (seconds since 2024-01-01 UTC, collision increments)
        public int GenerateTimeBasedId()
        {
            var baseDate = new System.DateTime(2024, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            int candidate = (int)(System.DateTime.UtcNow - baseDate).TotalSeconds;

            // Resolve collisions by incrementing until free
            int attempt = candidate;
            int safety = 0;
            while (GetById(attempt) != null)
            {
                attempt++;
                safety++;
                if (safety > 2000)
                    throw new System.Exception("Could not allocate a unique user ID after many attempts.");
            }
            return attempt;
        }

        // Existing dummy login
        public Utilizator GetByEmailAndPassword(string email, string password)
        {
            return _context.Utilizatori
                .FirstOrDefault(u => u.Email == email && u.Parola == password && u.EsteActiv == 1);
        }
    }
}
