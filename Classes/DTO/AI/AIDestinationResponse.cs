using System.Collections.Generic;

namespace MauiAppDisertatieVacantaAI.Classes.DTO.AI
{
    /// <summary>
    /// Model pentru răspunsul AI-ului când creează sugestii (cu destinație opțională)
    /// </summary>
    public class AIDestinationResponse
    {
        public string Action { get; set; } // "create_suggestion", "ask_preference", "general_chat", "error"
        public DestinationData Destination { get; set; } // DEPRECATED - păstrat pentru compatibilitate
        public SuggestionData Suggestion { get; set; } // Pentru planificări complete
        public List<DestinationSuggestion> Suggestions { get; set; } // NOU - pentru recomandări multiple (ask_preference)
        public string Message { get; set; } // Pentru utilizator
        public bool Success { get; set; }
    }

    /// <summary>
    /// Model pentru o recomandare de destinație (fără planificare completă)
    /// </summary>
    public class DestinationSuggestion
    {
        public string DestinatieDenumire { get; set; }
        public string DestinatieTara { get; set; }
        public string DestinatieOras { get; set; }
        public decimal BugetEstimat { get; set; }
        public string DescriereScurta { get; set; }
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
        
        // Lista de categorii (nume existente din DB)
        public List<string> Categorii { get; set; } = new List<string>();
    
        // Lista de facilități (pot fi noi sau existente)
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
        
        // NOU: Date pentru crearea destinației dacă nu există
        public DestinationData DestinatieData { get; set; }
    }

    public class PointOfInterestData
    {
        public string Denumire { get; set; }
        public string Descriere { get; set; }
        public string Tip { get; set; }
        public List<string> PhotoSearchQueries { get; set; } = new List<string>();
    }
}