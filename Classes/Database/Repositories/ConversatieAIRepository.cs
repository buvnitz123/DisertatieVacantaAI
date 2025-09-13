using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;
using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class ConversatieAIRepository : IRepository<ConversatieAI>
    {
        public IEnumerable<ConversatieAI> GetAll()
        {
            using var context = new AppContext();
            return context.ConversatiiAI.ToList();
        }

        public ConversatieAI GetById(int id)
        {
            using var context = new AppContext();
            return context.ConversatiiAI.Find(id);
        }

        public IEnumerable<ConversatieAI> GetByUserId(int userId)
        {
            using var context = new AppContext();
            return context.ConversatiiAI.Where(c => c.Id_Utilizator == userId).ToList();
        }

        // Generate next available ID using MAX(Id) + 1 strategy
        public int GenerateNextId()
        {
            try
            {
                using var context = new AppContext();
                // Get the maximum existing ID, default to 0 if no conversations exist
                var maxId = context.ConversatiiAI.Any() ? context.ConversatiiAI.Max(c => c.Id_ConversatieAI) : 0;
                var nextId = maxId + 1;
                Debug.WriteLine($"Generated next ConversatieAI ID: {nextId} (max was: {maxId})");
                return nextId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating next ConversatieAI ID: {ex.Message}");
                throw;
            }
        }

        public void Insert(ConversatieAI entity)
        {
            try
            {
                using var context = new AppContext();
                
                // Generate ID if not already set
                if (entity.Id_ConversatieAI == 0)
                {
                    entity.Id_ConversatieAI = GenerateNextId();
                }
                
                Debug.WriteLine($"Inserting ConversatieAI: Id={entity.Id_ConversatieAI}, Denumire={entity.Denumire}, Id_Utilizator={entity.Id_Utilizator}, Data_Creare={entity.Data_Creare}");
                
                context.ConversatiiAI.Add(entity);
                int result = context.SaveChanges();
                
                Debug.WriteLine($"SaveChanges returned: {result}");
                
                if (result == 0)
                {
                    throw new InvalidOperationException("No records were saved to the database.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ConversatieAIRepository.Insert: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void Update(ConversatieAI entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.ConversatiiAI.Find(id);
            if (entity != null)
            {
                context.ConversatiiAI.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
