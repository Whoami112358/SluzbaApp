using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Zolnierz_dane")]
    public class Zolnierz
    {
        [Key]
        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; } // ID żołnierza (login)

        [Column("Imie")]
        public string Imie { get; set; }

        [Column("Nazwisko")]
        public string Nazwisko { get; set; }

        [Column("Stopien")]
        public string Stopien { get; set; }

        [Column("Pesel")]
        public string Pesel { get; set; }

        [Column("ID_Pododdzialu")]
        public int IDPododdzialu { get; set; }

        [Column("Punkty")]
        public int Punkty { get; set; }

        // Nawigacja do Pododdziału
        [ForeignKey("IDPododdzialu")]
        public virtual Pododdzial Pododdzial { get; set; }
        public virtual Login LoginData { get; set; }
    }
}
