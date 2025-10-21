using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;
using System.Data.Entity;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class AppSettingsRepository : IRepository<AppSettings>
    {
        // Metode statice pentru acces rapid
        public static string GetValue(string key)
        {
            using var context = new AppContext();
            var setting = context.AppSettings.Find(key);
            return setting?.ParamValue;
        }

        public static void SetValue(string key, string value)
        {
            using var context = new AppContext();
            var setting = context.AppSettings.Find(key);
            if (setting != null)
            {
                setting.ParamValue = value;
                context.Entry(setting).State = EntityState.Modified;
            }
            else
            {
                context.AppSettings.Add(new AppSettings
                {
                    ParamKey = key,
                    ParamValue = value
                });
            }
            context.SaveChanges();
        }

        public static void DeleteValue(string key)
        {
            using var context = new AppContext();
            var entity = context.AppSettings.Find(key);
            if (entity != null)
            {
                context.AppSettings.Remove(entity);
                context.SaveChanges();
            }
        }

        // Metode instance (IRepository)
        public IEnumerable<AppSettings> GetAll()
        {
            using var context = new AppContext();
            return context.AppSettings.ToList();
        }

        public AppSettings GetById(int id)
        {
            return null;
        }

        public void Insert(AppSettings entity)
        {
            using var context = new AppContext();
            context.AppSettings.Add(entity);
            context.SaveChanges();
        }

        public void Update(AppSettings entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {

        }

        // Metode instance pentru backwards compatibility
        public string GetValueByKey(string key) => GetValue(key);
        public void SetValueByKey(string key, string value) => SetValue(key, value);
        public void DeleteByKey(string key) => DeleteValue(key);
    }
}
