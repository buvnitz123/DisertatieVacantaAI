using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Interfaces;
using MauiAppDisertatieVacantaAI.Classes.Library;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class CategorieVacantaRepository : IRepository<CategorieVacanta>
    {
        private readonly AppContext _context;

        public CategorieVacantaRepository()
        {
            _context = new AppContext();
        }

        public IEnumerable<CategorieVacanta> GetAll()
        {
            return _context.CategoriiVacanta.ToList();
        }

        public CategorieVacanta GetById(int id)
        {
            return _context.CategoriiVacanta.Find(id);
        }

        public void Insert(CategorieVacanta entity)
        {
            _context.CategoriiVacanta.Add(entity);
            _context.SaveChanges();
        }

        public void Update(CategorieVacanta entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                // Delete image from S3 if exists
                if (!string.IsNullOrEmpty(entity.ImagineUrl))
                {
                    try
                    {
                        S3Utils.DeleteImage(entity.ImagineUrl);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with database deletion
                        Debug.WriteLine($"Failed to delete image from S3: {ex.Message}");
                    }
                }

                _context.CategoriiVacanta.Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}
