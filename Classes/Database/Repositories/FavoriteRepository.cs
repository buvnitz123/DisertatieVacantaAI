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
        public IEnumerable<Favorite> GetAll()
        {
            using var context = new AppContext();
            return context.Favorite.ToList();
        }

        public Favorite GetById(int id)
        {
            // Favorite are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public void Insert(Favorite entity)
        {
            using var context = new AppContext();
            context.Favorite.Add(entity);
            context.SaveChanges();
        }

        public void Update(Favorite entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            // Favorite are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
