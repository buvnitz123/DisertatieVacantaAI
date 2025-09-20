using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;
using MauiAppDisertatieVacantaAI.Classes.Library.Services;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class CategorieVacantaRepository : IRepository<CategorieVacanta>
    {
        public IEnumerable<CategorieVacanta> GetAll()
        {
            using var context = new AppContext();
            return context.CategoriiVacanta.ToList();
        }

        public CategorieVacanta GetById(int id)
        {
            using var context = new AppContext();
            return context.CategoriiVacanta.Find(id);
        }

        public void Insert(CategorieVacanta entity)
        {
            using var context = new AppContext();
            context.CategoriiVacanta.Add(entity);
            context.SaveChanges();
        }

        public void Update(CategorieVacanta entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = new AppContext();
            var entity = context.CategoriiVacanta.Find(id);
            if (entity != null)
            {
                // Delete image from S3 if exists
                if (!string.IsNullOrEmpty(entity.ImagineUrl))
                {
                    try
                    {
                        AzureBlobService.DeleteImage(entity.ImagineUrl);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with database deletion
                        Debug.WriteLine($"Failed to delete image from S3: {ex.Message}");
                    }
                }

                context.CategoriiVacanta.Remove(entity);
                context.SaveChanges();
            }
        }
    }
}
