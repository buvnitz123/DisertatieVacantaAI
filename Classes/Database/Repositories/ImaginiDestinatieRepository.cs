using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class ImaginiDestinatieRepository : IRepository<ImaginiDestinatie>
    {
        public IEnumerable<ImaginiDestinatie> GetAll()
        {
            using var context = new AppContext();
            return context.ImaginiDestinatie.ToList();
        }

        public ImaginiDestinatie GetById(int id)
        {
            // ImaginiDestinatie are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public IEnumerable<ImaginiDestinatie> GetByDestinationId(int destinationId)
        {
            using var context = new AppContext();
            return context.ImaginiDestinatie
                .Where(i => i.Id_Destinatie == destinationId)
                .OrderBy(i => i.Id_ImaginiDestinatie)
                .ToList();
        }

        public void Insert(ImaginiDestinatie entity)
        {
            using var context = new AppContext();
            context.ImaginiDestinatie.Add(entity);
            context.SaveChanges();
        }

        public void Update(ImaginiDestinatie entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            // ImaginiDestinatie are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
