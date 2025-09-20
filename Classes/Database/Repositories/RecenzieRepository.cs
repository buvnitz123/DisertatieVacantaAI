using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class RecenzieRepository : IRepository<Recenzie>
    {
        public IEnumerable<Recenzie> GetAll()
        {
            using var context = new AppContext();
            return context.Recenzii.ToList();
        }

        public Recenzie GetById(int id)
        {
            using var context = new AppContext();
            return context.Recenzii.Find(id);
        }

        public void Insert(Recenzie entity)
        {
            using var context = new AppContext();
            
            // Generate manual ID since it's not auto-increment
            if (entity.Id_Recenzie == 0)
            {
                var maxId = context.Recenzii.Any() ? context.Recenzii.Max(r => r.Id_Recenzie) : 0;
                entity.Id_Recenzie = maxId + 1;
                Debug.WriteLine($"[RecenzieRepository] Generated new ID for review: {entity.Id_Recenzie}");
            }
            
            context.Recenzii.Add(entity);
            context.SaveChanges();
            
            Debug.WriteLine($"[RecenzieRepository] Successfully inserted review with ID: {entity.Id_Recenzie}");
        }

        public void Update(Recenzie entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.Recenzii.Find(id);
            if (entity != null)
            {
                context.Recenzii.Remove(entity);
                context.SaveChanges();
            }
        }

        // ✅ OPTIMIZED: Get reviews with related entities to avoid N+1
        public IEnumerable<Recenzie> GetByDestinationWithDetails(int destinationId)
        {
            using var context = new AppContext();
            return context.Recenzii
                .Include(r => r.Utilizator)
                .Include(r => r.Destinatie)
                .Where(r => r.Id_Destinatie == destinationId)
                .OrderByDescending(r => r.Data_Creare)
                .ToList();
        }

        // ✅ OPTIMIZED: Get reviews by user with details
        public IEnumerable<Recenzie> GetByUserWithDetails(int userId)
        {
            using var context = new AppContext();
            return context.Recenzii
                .Include(r => r.Destinatie)
                .Where(r => r.Id_Utilizator == userId)
                .OrderByDescending(r => r.Data_Creare)
                .ToList();
        }

        // ✅ NEW: Get reviews by destination ID (simple)
        public IEnumerable<Recenzie> GetByDestinationId(int destinationId)
        {
            using var context = new AppContext();
            return context.Recenzii
                .Where(r => r.Id_Destinatie == destinationId)
                .OrderByDescending(r => r.Data_Creare)
                .ToList();
        }

        // ✅ NEW: Get reviews by user ID (simple)
        public IEnumerable<Recenzie> GetByUserId(int userId)
        {
            using var context = new AppContext();
            return context.Recenzii
                .Where(r => r.Id_Utilizator == userId)
                .OrderByDescending(r => r.Data_Creare)
                .ToList();
        }
    }
}
