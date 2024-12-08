using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Zolnierz_dane")]
    public class Zolnierz
    {
        [Key]
        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("ID_Pododdzialu")]
        public int ID_Pododdzialu { get; set; }

        [Column("Imie")]
        public string Imie { get; set; }

        [Column("Nazwisko")]
        public string Nazwisko { get; set; }

        [Column("Stopien")]
        public string Stopien { get; set; }

        [Column("Wiek")]
        public int? Wiek { get; set; }

        [Column("Adres")]
        public string Adres { get; set; }

        [Column("Pesel")]
        public string Pesel { get; set; }

        [Column("Imie_ojca")]
        public string ImieOjca { get; set; }

        [Column("Punkty")]
        public int Punkty { get; set; }

        // Nawigacja do Pododdziału
        [ForeignKey("ID_Pododdzialu")]
        public virtual Pododdzial Pododdzial { get; set; }

        // Nawigacja do Login
        public virtual Login LoginData { get; set; }

        // Opcjonalnie: Nawigacje do innych tabel powiązanych
        public virtual ICollection<Harmonogram> Harmonogramy { get; set; }
        public virtual ICollection<Zwolnienie> Zwolnienia { get; set; }
        public virtual ICollection<Przewinienie> Przewinienia { get; set; }
        public virtual ICollection<Powiadomienie> Powiadomienia { get; set; }
        public virtual ICollection<Priorytet> Priorytety { get; set; }
    }
}
