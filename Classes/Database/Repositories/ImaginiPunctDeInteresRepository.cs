using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class ImaginiPunctDeInteresRepository : IRepository<ImaginiPunctDeInteres>
    {
        private readonly AppContext _context;

        public ImaginiPunctDeInteresRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<ImaginiPunctDeInteres> GetAll()
        {
            return _context.ImaginiPunctDeInteres.ToList();
        }

        public ImaginiPunctDeInteres GetById(int id)
        {
            // ImaginiPunctDeInteres are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public void Insert(ImaginiPunctDeInteres entity)
        {
            _context.ImaginiPunctDeInteres.Add(entity);
            _context.SaveChanges();
        }

        public void Update(ImaginiPunctDeInteres entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            // ImaginiPunctDeInteres are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }
    }
}
