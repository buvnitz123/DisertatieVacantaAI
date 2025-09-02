using System.Collections.Generic;

namespace MauiAppDisertatieVacantaAI.Classes.Library.PhotoAPIViews
{
    public class PexelsPhotoResponse
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalResults { get; set; }
        public string Url { get; set; }
        public List<PexelsPhoto> Photos { get; set; }
    }
}