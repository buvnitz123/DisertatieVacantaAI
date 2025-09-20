using MauiAppDisertatieVacantaAI.Classes.Database;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System.Diagnostics;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;

namespace MauiAppDisertatieVacantaAI.Classes.Database.Repositories
{
    public class FavoriteRepository : IRepository<Favorite>
    {
        public IEnumerable<Favorite> GetAll()
        {
            using var context = new AppContext();
            return context.Favorite.ToList();
        }

        public Favorite GetById(int id)
        {
            // Favorite are cheie compusă, deci GetById nu este aplicabil direct
            // Returnăm null pentru a indica că această operație nu este suportată
            return null;
        }

        public void Insert(Favorite entity)
        {
            using var context = new AppContext();
            context.Favorite.Add(entity);
            context.SaveChanges();
        }

        public void Update(Favorite entity)
        {
            using var context = new AppContext();
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            // Favorite are cheie compusă, deci Delete(int id) nu este aplicabil direct
            // Această metodă ar trebui să primească parametrii cheii compuse
        }

        // ✅ NEW: Metode specifice pentru favorite functionality
        
        /// <summary>
        /// Verifică dacă un element este favorit pentru un utilizator
        /// </summary>
        public bool IsFavorite(int userId, string tipElement, int idElement)
        {
            try
            {
                using var context = new AppContext();
                return context.Favorite.Any(f => 
                    f.Id_Utilizator == userId && 
                    f.TipElement == tipElement && 
                    f.Id_Element == idElement);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error checking favorite status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adaugă un element la favorite
        /// </summary>
        public void AddToFavorites(int userId, string tipElement, int idElement)
        {
            try
            {
                using var context = new AppContext();
                
                // Verifică dacă nu există deja
                var exists = context.Favorite.Any(f => 
                    f.Id_Utilizator == userId && 
                    f.TipElement == tipElement && 
                    f.Id_Element == idElement);
                
                if (!exists)
                {
                    var favorite = new Favorite
                    {
                        Id_Utilizator = userId,
                        TipElement = tipElement,
                        Id_Element = idElement
                    };
                    
                    context.Favorite.Add(favorite);
                    context.SaveChanges();
                    Debug.WriteLine($"[FavoriteRepository] Added to favorites: User={userId}, Type={tipElement}, Element={idElement}");
                }
                else
                {
                    Debug.WriteLine($"[FavoriteRepository] Already in favorites: User={userId}, Type={tipElement}, Element={idElement}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error adding to favorites: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Șterge un element din favorite
        /// </summary>
        public void RemoveFromFavorites(int userId, string tipElement, int idElement)
        {
            try
            {
                using var context = new AppContext();
                
                var favorite = context.Favorite.FirstOrDefault(f => 
                    f.Id_Utilizator == userId && 
                    f.TipElement == tipElement && 
                    f.Id_Element == idElement);
                
                if (favorite != null)
                {
                    context.Favorite.Remove(favorite);
                    context.SaveChanges();
                    Debug.WriteLine($"[FavoriteRepository] Removed from favorites: User={userId}, Type={tipElement}, Element={idElement}");
                }
                else
                {
                    Debug.WriteLine($"[FavoriteRepository] Not found in favorites: User={userId}, Type={tipElement}, Element={idElement}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error removing from favorites: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obține toate favoritele unui utilizator
        /// </summary>
        public IEnumerable<Favorite> GetUserFavorites(int userId)
        {
            try
            {
                using var context = new AppContext();
                return context.Favorite
                    .Where(f => f.Id_Utilizator == userId)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error getting user favorites: {ex.Message}");
                return Enumerable.Empty<Favorite>();
            }
        }

        /// <summary>
        /// Obține favoritele unui utilizator filtrate după tip
        /// </summary>
        public IEnumerable<Favorite> GetUserFavoritesByType(int userId, string tipElement)
        {
            try
            {
                using var context = new AppContext();
                return context.Favorite
                    .Where(f => f.Id_Utilizator == userId && f.TipElement == tipElement)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error getting user favorites by type: {ex.Message}");
                return Enumerable.Empty<Favorite>();
            }
        }

        /// <summary>
        /// Toggle favorite status (add if not exists, remove if exists)
        /// </summary>
        public bool ToggleFavorite(int userId, string tipElement, int idElement)
        {
            try
            {
                if (IsFavorite(userId, tipElement, idElement))
                {
                    RemoveFromFavorites(userId, tipElement, idElement);
                    return false; // Removed from favorites
                }
                else
                {
                    AddToFavorites(userId, tipElement, idElement);
                    return true; // Added to favorites
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error toggling favorite: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obține batch de favorite pentru verificare eficientă
        /// </summary>
        public Dictionary<int, bool> GetFavoritesStatusBatch(int userId, string tipElement, IEnumerable<int> elementIds)
        {
            try
            {
                using var context = new AppContext();
                var elementIdsList = elementIds.ToList();
                
                var favorites = context.Favorite
                    .Where(f => f.Id_Utilizator == userId && 
                               f.TipElement == tipElement && 
                               elementIdsList.Contains(f.Id_Element))
                    .Select(f => f.Id_Element)
                    .ToList();
                
                return elementIdsList.ToDictionary(id => id, id => favorites.Contains(id));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FavoriteRepository] Error getting favorites batch: {ex.Message}");
                return elementIds.ToDictionary(id => id, id => false);
            }
        }
    }
}
