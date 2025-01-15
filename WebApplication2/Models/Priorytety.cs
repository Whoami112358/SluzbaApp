using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication2.Models
{
    [Table("Priorytety_dane")]
    public class Priorytet
    {
        [Key]
        [Column("ID_Priorytetu")]
        public int ID_Priorytetu { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("ID_Sluzby")]
        public int ID_Sluzby { get; set; }

        [Column("Priorytet")]
        public int? PriorytetValue { get; set; } // Wartość priorytetu

        // Nawigacja do Zolnierz
        [ForeignKey("ID_Zolnierza")]
        [ValidateNever] // Dodane, aby model binder nie próbował mapować tej właściwości
        public virtual Zolnierz Zolnierz { get; set; }

        // Nawigacja do Sluzba
        [ForeignKey("ID_Sluzby")]
        [ValidateNever] // Dodane, aby model binder nie próbował mapować tej właściwości
        public virtual Sluzba Sluzba { get; set; }
    }
}
