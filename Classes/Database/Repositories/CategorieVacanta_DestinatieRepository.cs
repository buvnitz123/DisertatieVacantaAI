using MauiAppDisertatieVacantaAI.Classes.Database;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class CategorieVacanta_DestinatieRepository : IRepository<CategorieVacanta_Destinatie>
    {
        public IEnumerable<CategorieVacanta_Destinatie> GetAll()
        {
            using var context = new AppContext();
            return context.CategorieVacanta_Destinatie.ToList();
        }

        public CategorieVacanta_Destinatie GetById(int id)
        {
            // CategorieVacanta_Destinatie are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public void Insert(CategorieVacanta_Destinatie entity)
        {
            using var context = new AppContext();
            context.CategorieVacanta_Destinatie.Add(entity);
            context.SaveChanges();
        }

        public void Update(CategorieVacanta_Destinatie entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            // CategorieVacanta_Destinatie are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
