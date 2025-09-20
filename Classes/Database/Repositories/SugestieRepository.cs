using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class SugestieRepository : IRepository<Sugestie>
    {
        // Remove the persistent context field to prevent memory leaks

        public IEnumerable<Sugestie> GetAll()
        {
            using var context = new AppContext();
            return context.Sugestii.ToList();
        }

        public Sugestie GetById(int id)
        {
            using var context = new AppContext();
            return context.Sugestii.Find(id);
        }

        public void Insert(Sugestie entity)
        {
            using var context = new AppContext();
            
            // Generate new ID as max existing ID + 1
            var maxId = context.Sugestii.Any() ? context.Sugestii.Max(s => s.Id_Sugestie) : 0;
            entity.Id_Sugestie = maxId + 1;
            
            context.Sugestii.Add(entity);
            context.SaveChanges();
        }

        public void Update(Sugestie entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.Sugestii.Find(id);
            if (entity != null)
            {
                context.Sugestii.Remove(entity);
                context.SaveChanges();
            }
        }

        // ? FIX N+1: Eager load Destinatie to avoid multiple queries
        public IEnumerable<Sugestie> GetByUser(int userId)
        {
            using var context = new AppContext();
            return context.Sugestii
                .Include(s => s.Destinatie) // ? EAGER LOADING - eliminates N+1!
                .Where(s => s.Id_Utilizator == userId)
                .OrderByDescending(s => s.Data_Inregistrare)
                .ToList();
        }

        // ? BONUS: Get single suggestion with destination (avoids extra query)
        public Sugestie GetByIdWithDestination(int id)
        {
            using var context = new AppContext();
            return context.Sugestii
                .Include(s => s.Destinatie)
                .FirstOrDefault(s => s.Id_Sugestie == id);
        }

        // ? BONUS: Get all suggestions with destinations for performance
        public IEnumerable<Sugestie> GetAllWithDestinations()
        {
            using var context = new AppContext();
            return context.Sugestii
                .Include(s => s.Destinatie)
                .OrderByDescending(s => s.Data_Inregistrare)
                .ToList();
        }
    }
}
