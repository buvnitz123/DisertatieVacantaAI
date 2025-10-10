using System.Collections.Generic;

namespace MauiAppDisertatieVacantaAI.Classes.DTO.AI
{
    /// <summary>
    /// Model pentru răspunsul AI-ului când creează destinații
    /// </summary>
    public class AIDestinationResponse
    {
        public string Action { get; set; } // "create_destination", "destination_exists", "error"
        public DestinationData Destination { get; set; }
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

    public class PointOfInterestData
    {
        public string Denumire { get; set; }
        public string Descriere { get; set; }
        public string Tip { get; set; }
        public List<string> PhotoSearchQueries { get; set; } = new List<string>();
    }
}