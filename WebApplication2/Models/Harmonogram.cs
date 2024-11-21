namespace WebApplication2.Models
{
    public class Harmonogram
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string RodzajSluzby { get; set; }
        public int ZolnierzId { get; set; }
        public Zolnierz Zolnierz { get; set; }
    }
}
