namespace WebApplication2.Models
{
    public class Zolnierz
    {
        public int Id { get; set; }
        public string NazwaUzytkownika { get; set; }
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public ICollection<Harmonogram> Harmonogramy { get; set; }
    }
}
