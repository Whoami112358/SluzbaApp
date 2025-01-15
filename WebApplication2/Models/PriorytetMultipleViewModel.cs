using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class PriorytetMultipleViewModel
    {
        [Required(ErrorMessage = "Wybierz priorytet dla Warty.")]
        [Range(1, 5, ErrorMessage = "Priorytet musi być w zakresie od 1 do 5.")]
        public int PriorytetWarta { get; set; }

        [Required(ErrorMessage = "Wybierz priorytet dla Patrolu.")]
        [Range(1, 5, ErrorMessage = "Priorytet musi być w zakresie od 1 do 5.")]
        public int PriorytetPatrol { get; set; }

        [Required(ErrorMessage = "Wybierz priorytet dla Pododdziału.")]
        [Range(1, 5, ErrorMessage = "Priorytet musi być w zakresie od 1 do 5.")]
        public int PriorytetPododdzial { get; set; }
    }
}
