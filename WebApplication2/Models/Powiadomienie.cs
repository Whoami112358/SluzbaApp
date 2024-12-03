using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Powiadomienia_dane")]
    public class Powiadomienie
    {
        [Key]
        [Column("ID_Powiadomienia")]
        public int ID_Powiadomienia { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("Tresc_powiadomienia")]
        public string TrescPowiadomienia { get; set; }

        [Column("Typ_powiadomienia")]
        public string TypPowiadomienia { get; set; }

        [Column("Data_i_godzina_wyslania")]
        public DateTime? DataIGodzinaWyslania { get; set; }

        [Column("Status")]
        public string Status { get; set; }

        // Nawigacja do Zolnierz
        [ForeignKey("ID_Zolnierza")]
        public virtual Zolnierz Zolnierz { get; set; }
    }
}
