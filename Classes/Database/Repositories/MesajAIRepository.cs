using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class MesajAIRepository : IRepository<MesajAI>
    {
        public IEnumerable<MesajAI> GetAll()
        {
            using var context = new AppContext();
            return context.MesajeAI.ToList();
        }

        public MesajAI GetById(int id)
        {
            using var context = new AppContext();
            return context.MesajeAI.Find(id);
        }

        public IEnumerable<MesajAI> GetByConversationId(int conversationId)
        {
            using var context = new AppContext();
            return context.MesajeAI.Where(m => m.Id_ConversatieAI == conversationId).ToList();
        }

        // Generate next available ID using MAX(Id) + 1 strategy
        public int GenerateNextId()
        {
            try
            {
                using var context = new AppContext();
                // Get the maximum existing ID, default to 0 if no messages exist
                var maxId = context.MesajeAI.Any() ? context.MesajeAI.Max(m => m.Id_Mesaj) : 0;
                var nextId = maxId + 1;
                Debug.WriteLine($"Generated next MesajAI ID: {nextId} (max was: {maxId})");
                return nextId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating next MesajAI ID: {ex.Message}");
                throw;
            }
        }

        public void Insert(MesajAI entity)
        {
            try
            {
                using var context = new AppContext();
                
                // Generate ID if not already set
                if (entity.Id_Mesaj == 0)
                {
                    entity.Id_Mesaj = GenerateNextId();
                }
                
                Debug.WriteLine($"Inserting MesajAI: Id={entity.Id_Mesaj}, Mesaj={entity.Mesaj}, Id_ConversatieAI={entity.Id_ConversatieAI}, Mesaj_User={entity.Mesaj_User}, Data_Creare={entity.Data_Creare}");
                
                context.MesajeAI.Add(entity);
                int result = context.SaveChanges();
                
                Debug.WriteLine($"SaveChanges returned: {result}");
                
                if (result == 0)
                {
                    throw new InvalidOperationException("No records were saved to the database.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MesajAIRepository.Insert: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void Update(MesajAI entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.MesajeAI.Find(id);
            if (entity != null)
            {
                context.MesajeAI.Remove(entity);
                context.SaveChanges();
            }
        }

        public void DeleteByConversationId(int conversationId)
        {
            try
            {
                using var context = new AppContext();
                var messages = context.MesajeAI.Where(m => m.Id_ConversatieAI == conversationId).ToList();
                
                Debug.WriteLine($"Deleting {messages.Count} messages for conversation ID: {conversationId}");
                
                if (messages.Any())
                {
                    context.MesajeAI.RemoveRange(messages);
                    int result = context.SaveChanges();
                    Debug.WriteLine($"Deleted {result} messages successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MesajAIRepository.DeleteByConversationId: {ex.Message}");
                throw;
            }
        }
    }
}
