using MauiAppDisertatieVacantaAI.Classes.Database;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

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

        // Metodă nouă pentru căutare după categoria
        public IEnumerable<CategorieVacanta_Destinatie> GetByCategoryId(int categoryId)
        {
            using var context = new AppContext();
            return context.CategorieVacanta_Destinatie
                .Where(cd => cd.Id_CategorieVacanta == categoryId)
                .ToList();
        }

        // Metodă nouă pentru căutare după destinația
        public IEnumerable<CategorieVacanta_Destinatie> GetByDestinationId(int destinationId)
        {
            using var context = new AppContext();
            return context.CategorieVacanta_Destinatie
                .Where(cd => cd.Id_Destinatie == destinationId)
                .ToList();
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

        // Metodă pentru ștergere cu cheie compusă
        public void Delete(int destinationId, int categoryId)
        {
            using var context = new AppContext();
            var entity = context.CategorieVacanta_Destinatie
                .FirstOrDefault(cd => cd.Id_Destinatie == destinationId && cd.Id_CategorieVacanta == categoryId);
            if (entity != null)
            {
                context.CategorieVacanta_Destinatie.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
