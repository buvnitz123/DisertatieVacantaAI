using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;
using MauiAppDisertatieVacantaAI.Classes.Library; // added

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class UtilizatorRepository : IRepository<Utilizator>
    {
        // Remove the persistent context field to prevent memory leaks
        
        // Force EF model initialization to avoid first-hit delays later
        public void Initialize()
        {
            using var context = new AppContext();
            // Ensure the context and model are initialized without forcing reinitialization
            context.Database.Initialize(false);
        }

        public IEnumerable<Utilizator> GetAll()
        {
            using var context = new AppContext();
            return context.Utilizatori.ToList();
        }

        public Utilizator GetById(int id)
        {
            using var context = new AppContext();
            return context.Utilizatori.Find(id);
        }

        public Utilizator GetByEmail(string email)
        {
            using var context = new AppContext();
            return context.Utilizatori.FirstOrDefault(u => u.Email == email);
        }

        public bool EmailExists(string email)
        {
            using var context = new AppContext();
            return context.Utilizatori.Any(u => u.Email == email);
        }

        public void Insert(Utilizator entity)
        {
            using var context = new AppContext();
            context.Utilizatori.Add(entity);
            context.SaveChanges();
        }

        public void Update(Utilizator entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.Utilizatori.Find(id);
            if (entity != null)
            {
                context.Utilizatori.Remove(entity);
                context.SaveChanges();
            }
        }

        // Generate next available ID using MAX(Id) + 1 strategy
        public int GenerateNextId()
        {
            try
            {
                using var context = new AppContext();
                // Get the maximum existing ID, default to 0 if no users exist
                var maxId = context.Utilizatori.Any() 
                    ? context.Utilizatori.Max(u => u.Id_Utilizator) 
                    : 0;
                
                return maxId + 1;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating next ID: {ex.Message}");
                // Fallback to timestamp-based approach if MAX query fails
                var baseDate = new System.DateTime(2024, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                return (int)(System.DateTime.UtcNow - baseDate).TotalSeconds;
            }
        }

        // Encrypted password login (fallback to legacy plain)
        public Utilizator GetByEmailAndPassword(string email, string password)
        {
            using var context = new AppContext();
            string encrypted = EncryptionUtils.Encrypt(password);
            return context.Utilizatori
                .FirstOrDefault(u =>
                    u.Email == email &&
                    u.EsteActiv == 1 &&
                    (u.Parola == encrypted || u.Parola == password)); // fallback for old records
        }
    }
}
