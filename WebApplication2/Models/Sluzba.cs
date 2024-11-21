using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Sluzba_dane")]
    public class Sluzba
    {
        [Key]
        [Column("ID_Sluzby")]
        public int ID_Sluzby { get; set; }

        [Column("Rodzaj")]
        public string Rodzaj { get; set; }

        [Column("Przelozony")]
        public string Przelozony { get; set; }

        [Column("Miejsce_pelnienia_sluzby")]
        public string MiejscePelnieniaSluzby { get; set; }
    }
}
