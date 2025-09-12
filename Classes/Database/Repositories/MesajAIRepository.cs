using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

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

        public void Insert(MesajAI entity)
        {
            using var context = new AppContext();
            context.MesajeAI.Add(entity);
            context.SaveChanges();
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
    }
}
