using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Przewinienia_dane")]
    public class Przewinienie
    {
        [Key]
        [Column("ID_Przeinienia")]
        public int ID_Przeinienia { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("Liczba_dodatkowych_sluzb")]
        public int? LiczbaDodatkowychSluzb { get; set; }

        [Column("Data_wprowadzenia")]
        public DateTime? DataWprowadzenia { get; set; }

        [Column("Opis_przewinienia")]
        public string OpisPrzewinienia { get; set; }

        // Nawigacja do Zolnierz
        [ForeignKey("ID_Zolnierza")]
        public virtual Zolnierz Zolnierz { get; set; }
    }
}
