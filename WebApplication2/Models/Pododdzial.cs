using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Pododdzial_dane")]
    public class Pododdzial
    {
        [Key]
        [Column("ID_Pododdzialu")]
        public int ID_Pododdzialu { get; set; }

        [Column("Nazwa")]
        public string Nazwa { get; set; }

        [Column("Komorka_nadrzedna")]
        public string KomorkaNadrzedna { get; set; }

        [Column("Dowodca")]
        public string Dowodca { get; set; }

        // Lista żołnierzy przypisanych do tego pododdziału
        public virtual ICollection<Zolnierz> Zolnierze { get; set; }

        // Lista harmonogramów przypisanych do tego pododdziału
        public virtual ICollection<Harmonogram> Harmonogramy { get; set; }
    }
}
