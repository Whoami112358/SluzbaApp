using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Zwolnienia_dane")]
    public class Zwolnienie
    {
        [Key]
        [Column("ID_Zwolnienia")]
        public int ID_Zwolnienia { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("Data_rozpoczecia_zwolnienia")]
        public DateTime? DataRozpoczeciaZwolnienia { get; set; }

        [Column("Data_zakonczenia_zwolnienia")]
        public DateTime? DataZakonczeniaZwolnienia { get; set; }

        // Nawigacja do Zolnierz
        [ForeignKey("ID_Zolnierza")]
        public virtual Zolnierz Zolnierz { get; set; }
    }
}
