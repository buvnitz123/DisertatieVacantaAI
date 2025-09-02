namespace MauiAppDisertatieVacantaAI.Classes.Library.PhotoAPIViews
{
    public class PexelsPhoto 
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Url { get; set; }
        public string Photographer { get; set; }
        public string PhotographerUrl { get; set; }
        public int PhotographerId { get; set; }
        public PexelsPhotoSource Src { get; set; }
        public string Alt { get; set; }
    }
}