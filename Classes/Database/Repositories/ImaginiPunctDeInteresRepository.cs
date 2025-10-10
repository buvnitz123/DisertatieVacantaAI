using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class ImaginiPunctDeInteresRepository : IRepository<ImaginiPunctDeInteres>
    {
        public IEnumerable<ImaginiPunctDeInteres> GetAll()
        {
            using var context = new AppContext();
            return context.ImaginiPunctDeInteres.ToList();
        }

        public ImaginiPunctDeInteres GetById(int id)
        {
            // ImaginiPunctDeInteres are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public IEnumerable<ImaginiPunctDeInteres> GetByPointOfInterestId(int poiId)
        {
            using var context = new AppContext();
            return context.ImaginiPunctDeInteres
                .Where(i => i.Id_PunctDeInteres == poiId)
                .OrderBy(i => i.Id_ImaginiPunctDeInteres)
                .ToList();
        }

        public void Insert(ImaginiPunctDeInteres entity)
        {
            using var context = new AppContext();
            
            // Generate next ID for this point of interest (MAX + 1 per POI)
            var maxId = context.ImaginiPunctDeInteres
                .Where(i => i.Id_PunctDeInteres == entity.Id_PunctDeInteres)
                .Any() ? 
                context.ImaginiPunctDeInteres
                    .Where(i => i.Id_PunctDeInteres == entity.Id_PunctDeInteres)
                    .Max(i => i.Id_ImaginiPunctDeInteres) : 0;
            
            entity.Id_ImaginiPunctDeInteres = maxId + 1;
            
            System.Diagnostics.Debug.WriteLine($"Generated next ImaginiPunctDeInteres ID: {entity.Id_ImaginiPunctDeInteres} for POI {entity.Id_PunctDeInteres} (max was: {maxId})");
            
            context.ImaginiPunctDeInteres.Add(entity);
            context.SaveChanges();
        }

        public void Update(ImaginiPunctDeInteres entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            // ImaginiPunctDeInteres are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
