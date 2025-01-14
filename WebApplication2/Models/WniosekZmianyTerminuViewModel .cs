using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class WniosekZmianyTerminuViewModel
    {
        public int ID_Harmonogramu { get; set; }

        [Required]
        public string Uzasadnienie { get; set; }

      
        public string ProponowanaData { get; set; }
    }
}
