using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using MauiAppDisertatieVacantaAI.Classes.Enums;
using System.Text;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Services
{
    public class RecommendationService
    {
        private readonly LogActivitateRepository _logRepo;
        private readonly DestinatieRepository _destinatieRepo;
        private readonly CategorieVacantaRepository _catRepo;
        private readonly CategorieVacanta_DestinatieRepository _catDestRepo;
        private readonly PunctDeInteresRepository _poiRepo;

        public RecommendationService()
        {
            _logRepo = new LogActivitateRepository();
            _destinatieRepo = new DestinatieRepository();
            _catRepo = new CategorieVacantaRepository();
            _catDestRepo = new CategorieVacanta_DestinatieRepository();
            _poiRepo = new PunctDeInteresRepository();
        }

        // 1. Calcularea Scorul per Entitate (ex: Destinatii)
        public Dictionary<int, int> GetEntityScores(int userId, TipEntitate tipEntitate)
        {
            var logs = _logRepo.GetAll().Where(l => l.Id_Utilizator == userId && l.TipEntitate == tipEntitate.ToString() && l.IdEntitate.HasValue).ToList();
            var scores = new Dictionary<int, int>();

            foreach (var log in logs)
            {
                int points = 0;
                if (log.TipActivitate == TipActivitate.Favorit.ToString()) points = 10;
                else if (log.TipActivitate == TipActivitate.CreareAI.ToString()) points = 7;
                else if (log.TipActivitate == TipActivitate.Creare.ToString()) points = 7;
                else if (log.TipActivitate == TipActivitate.Cautare.ToString()) points = 3;
                else if (log.TipActivitate == TipActivitate.Vizitare.ToString()) points = 1;

                // Time decay simplu: activitatea mai veche de o luna primeste jumatate de punctaj
                if (log.DataInregistrare.HasValue && (DateTime.Now - log.DataInregistrare.Value).TotalDays > 30)
                {
                    points = (int)Math.Max(1, points * 0.5);
                }

                if (!scores.ContainsKey(log.IdEntitate.Value))
                    scores[log.IdEntitate.Value] = 0;

                scores[log.IdEntitate.Value] += points;
            }

            return scores.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        // 2. Extragerea unui text pentru Prompt-ul AI
        public string GetUserProfileSummaryText(int userId)
        {
            var dateDest = GetEntityScores(userId, TipEntitate.Destinatie).Take(3).Select(k => k.Key).ToList();
            if (!dateDest.Any()) return string.Empty;

            var destinatiiAlese = _destinatieRepo.GetAll().Where(d => dateDest.Contains(d.Id_Destinatie)).Select(d => d.Denumire).ToList();

            var categScores = GetEntityScores(userId, TipEntitate.CategorieVacanta).Take(2).Select(k => k.Key).ToList();
            var categAlese = _catRepo.GetAll().Where(c => categScores.Contains(c.Id_CategorieVacanta)).Select(c => c.Denumire).ToList();

            StringBuilder sb = new StringBuilder();
            sb.Append("Acest utilizator prezinta un interes deosebit pentru urmatoarele: ");
            if (categAlese.Any())
            {
                sb.Append($"Categoriile de vacanta '{string.Join(", ", categAlese)}'. ");
            }
            if (destinatiiAlese.Any())
            {
                sb.Append($"Destinatii apreciate recent includ '{string.Join(", ", destinatiiAlese)}'. ");
            }
            sb.Append("Pastreaza acest context pentru a-i recomanda sugestii cat mai potrivite si personalizate.");

            return sb.ToString();
        }

        // 3. Recomendare Destinatii pentru MainPage
        public List<Destinatie> GetRecommendedDestinations(int userId, int limit = 5)
        {
            var userScores = GetEntityScores(userId, TipEntitate.Destinatie);
            var allDestinations = _destinatieRepo.GetAll().ToList();
            
            // Daca nu are suficiente interacțiuni, dam random
            if (!userScores.Any())
            {
                return allDestinations.OrderBy(x => Guid.NewGuid()).Take(limit).ToList();
            }

            var recommendedIds = userScores.Keys.Take(limit).ToList();
            var result = allDestinations.Where(d => recommendedIds.Contains(d.Id_Destinatie)).ToList();
            
            // Putem extinde mai tarziu recomandarea pe baza Categoriilor daca gasim putine.
            // Daca rezultatul este sub limit, completam cu random:
            if (result.Count < limit)
            {
                var extraseId = result.Select(r => r.Id_Destinatie).ToList();
                var fill = allDestinations.Where(d => !extraseId.Contains(d.Id_Destinatie)).OrderBy(x => Guid.NewGuid()).Take(limit - result.Count).ToList();
                result.AddRange(fill);
            }
            return result;
        }

        public List<CategorieVacanta> GetRecommendedCategories(int userId, int limit = 4)
        {
            var userScores = GetEntityScores(userId, TipEntitate.CategorieVacanta);
            var allCategories = _catRepo.GetAll().ToList();
            
            if (!userScores.Any())
            {
                return allCategories.OrderBy(x => Guid.NewGuid()).Take(limit).ToList();
            }

            var recommendedIds = userScores.Keys.Take(limit).ToList();
            var result = allCategories.Where(c => recommendedIds.Contains(c.Id_CategorieVacanta)).ToList();
            
            if (result.Count < limit)
            {
                var extraseId = result.Select(r => r.Id_CategorieVacanta).ToList();
                var fill = allCategories.Where(c => !extraseId.Contains(c.Id_CategorieVacanta)).OrderBy(x => Guid.NewGuid()).Take(limit - result.Count).ToList();
                result.AddRange(fill);
            }
            return result;
        }

        public List<PunctDeInteres> GetRecommendedPointsOfInterest(int userId, int limit = 5)
        {
            var userScores = GetEntityScores(userId, TipEntitate.PunctDeInteres);
            var allPois = _poiRepo.GetAll().ToList();
            
            if (!userScores.Any())
            {
                return allPois.OrderBy(x => Guid.NewGuid()).Take(limit).ToList();
            }

            var recommendedIds = userScores.Keys.Take(limit).ToList();
            var result = allPois.Where(p => recommendedIds.Contains(p.Id_PunctDeInteres)).ToList();
            
            if (result.Count < limit)
            {
                var extraseId = result.Select(r => r.Id_PunctDeInteres).ToList();
                var fill = allPois.Where(p => !extraseId.Contains(p.Id_PunctDeInteres)).OrderBy(x => Guid.NewGuid()).Take(limit - result.Count).ToList();
                result.AddRange(fill);
            }
            return result;
        }
    }
}
