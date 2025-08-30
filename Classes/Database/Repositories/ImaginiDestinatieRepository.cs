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
        private readonly AppContext _context;

        public ImaginiDestinatieRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<ImaginiDestinatie> GetAll()
        {
            return _context.ImaginiDestinatie.ToList();
        }

        public ImaginiDestinatie GetById(int id)
        {
            // ImaginiDestinatie are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public void Insert(ImaginiDestinatie entity)
        {
            _context.ImaginiDestinatie.Add(entity);
            _context.SaveChanges();
        }

        public void Update(ImaginiDestinatie entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            // ImaginiDestinatie are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
