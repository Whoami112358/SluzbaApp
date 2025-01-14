using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Harmonogram_dane")]
    public class Harmonogram
    {
        [Key]
        [Column("ID_Harmonogram")]
        public int ID_Harmonogram { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("ID_Sluzby")]
        public int ID_Sluzby { get; set; }

        [Column("Data")]
        public DateTime Data { get; set; }

        [Column("Miesiac")]
        public string Miesiac { get; set; }

        // Nawigacja do Zolnierz
        [ForeignKey("ID_Zolnierza")]
        public virtual Zolnierz Zolnierz { get; set; }

        // Nawigacja do Sluzba
        [ForeignKey("ID_Sluzby")]
        public virtual Sluzba Sluzba { get; set; }

        // Nawigacja do Zastepca
        public virtual ICollection<Zastepca> Zastepcy { get; set; }
    }
}
