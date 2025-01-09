using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Login_dane")]
    public class Login
    {
        [Key]
        [Column("ID_Loginu")]
        public int ID_Loginu { get; set; }

        [Column("ID_Zolnierza")]
        public int ID_Zolnierza { get; set; }

        [Column("Login")]
        public string LoginName { get; set; }

        [Column("Haslo")]
        public string Haslo { get; set; }

        [Column("Email")]
        public string Email { get; set; }

        // Nawigacja do Zolnierz
        [ForeignKey("ID_Zolnierza")]
        public virtual Zolnierz Zolnierz { get; set; }
    }
}
