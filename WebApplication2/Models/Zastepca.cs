using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Zastepca_dane")]
    public class Zastepca
    {
        [Key]
        [Column("ID_Zastepca")]
        public int ID_Zastepca { get; set; }

        [Column("ID_Harmonogram")]
        public int ID_Harmonogram { get; set; }

        [Column("ID_Zolnierza_Zastepowanego")] // Poprawiona nazwa
        public int ID_Zolnierza_Zastepowanego { get; set; }

        [Column("Data_Przydzielenia")]
        public DateTime DataPrzydzielenia { get; set; }

        // Nawigacja do Harmonogramu
        [ForeignKey("ID_Harmonogram")]
        public virtual Harmonogram Harmonogram { get; set; }

        // Nawigacja do Zolnierza (zastępcy)
        [ForeignKey("ID_Zolnierza_Zastepowanego")]
        public virtual Zolnierz ZolnierzZastepowanego { get; set; }
    }
}
