using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class DestinatieFacilitateRepository : IRepository<DestinatieFacilitate>
    {
        public IEnumerable<DestinatieFacilitate> GetAll()
        {
            using var context = new AppContext();
            return context.DestinatieFacilitate.ToList();
        }

        public DestinatieFacilitate GetById(int id)
        {
            // DestinatieFacilitate are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        // Metodă nouă pentru căutare după facilitatea
        public IEnumerable<DestinatieFacilitate> GetByFacilityId(int facilityId)
        {
            using var context = new AppContext();
            return context.DestinatieFacilitate
                .Where(df => df.Id_Facilitate == facilityId)
                .ToList();
        }

        // Metodă nouă pentru căutare după destinația
        public IEnumerable<DestinatieFacilitate> GetByDestinationId(int destinationId)
        {
            using var context = new AppContext();
            return context.DestinatieFacilitate
                .Where(df => df.Id_Destinatie == destinationId)
                .ToList();
        }

        public void Insert(DestinatieFacilitate entity)
        {
            using var context = new AppContext();
            context.DestinatieFacilitate.Add(entity);
            context.SaveChanges();
        }

        public void Update(DestinatieFacilitate entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            // DestinatieFacilitate are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }

        // Metodă pentru ștergere cu cheie compusă
        public void Delete(int destinationId, int facilityId)
        {
            using var context = new AppContext();
            var entity = context.DestinatieFacilitate
                .FirstOrDefault(df => df.Id_Destinatie == destinationId && df.Id_Facilitate == facilityId);
            if (entity != null)
            {
                context.DestinatieFacilitate.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
