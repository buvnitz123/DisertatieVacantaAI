using System.Collections.Generic;

namespace MauiAppDisertatieVacantaAI.Classes.DTO.AI
{
    /// <summary>
    /// Model pentru răspunsul AI-ului când creează destinații SAU sugestii
    /// </summary>
    public class AIDestinationResponse
    {
        public string Action { get; set; } // "create_destination", "create_suggestion", "destination_exists", "general_chat", "error"
        public DestinationData Destination { get; set; }
        public SuggestionData Suggestion { get; set; } // NOU - pentru sugestii
        public string Message { get; set; } // Pentru utilizator
        public bool Success { get; set; }
    }

    public class DestinationData
    {
        public string Denumire { get; set; }
        public string Tara { get; set; }
        public string Oras { get; set; }
        public string Regiune { get; set; }
        public string Descriere { get; set; }
        public decimal PretAdult { get; set; }
        public decimal PretMinor { get; set; }
        
        // Lista de categorii (IDs existente sau nume noi)
        public List<string> Categorii { get; set; } = new List<string>();
    
        // Lista de facilități (IDs existente sau nume noi)
        public List<string> Facilitati { get; set; } = new List<string>();
        
        // Lista de puncte de interes
        public List<PointOfInterestData> PuncteDeInteres { get; set; } = new List<PointOfInterestData>();
        
        // Pentru Pexels search
        public List<string> PhotoSearchQueries { get; set; } = new List<string>();
    }

    /// <summary>
    /// Model pentru datele unei sugestii generate de AI
    /// </summary>
    public class SuggestionData
    {
        public string Titlu { get; set; } // Ex: "City Break Paris 4 zile"
        public decimal BugetEstimat { get; set; } // Buget total estimat
        public string Descriere { get; set; } // Planificarea detaliată zi cu zi
        public string DestinatieDenumire { get; set; } // Numele destinației (pentru căutare în DB)
        public string DestinatieTara { get; set; } // Țara destinației
        public string DestinatieOras { get; set; } // Orașul destinației
        public int? EstePublic { get; set; } // 0 = privat, 1 = public (default 0)
    }

    public class PointOfInterestData
    {
        public string Denumire { get; set; }
        public string Descriere { get; set; }
        public string Tip { get; set; }
        public List<string> PhotoSearchQueries { get; set; } = new List<string>();
    }
}