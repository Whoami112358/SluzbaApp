using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Urlopy_dane")]
    public class Urlop
    {
        [Key]
        [Column("ID_Urlopu")]
        public int ID_Urlopu { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("Data_rozpoczecia")]
        public DateTime DataRozpoczecia { get; set; }

        [Column("Data_zakonczenia")]
        public DateTime DataZakonczenia { get; set; }

        [ForeignKey("ID_Zolnierza")]
        public virtual Zolnierz Zolnierz { get; set; }
    }
}
