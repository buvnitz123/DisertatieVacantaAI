using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

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

        public void Insert(ConversatieAI entity)
        {
            using var context = new AppContext();
            context.ConversatiiAI.Add(entity);
            context.SaveChanges();
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
