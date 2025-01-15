using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class PrzydzielAutomatycznieViewModel
    {
        [Required(ErrorMessage = "Kryterium przydziału jest wymagane.")]
        [Display(Name = "Kryterium przydziału")]
        public string WybraneKryterium { get; set; }

        [Required(ErrorMessage = "Miesiąc jest wymagany.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])-(20\d{2})$", ErrorMessage = "Format miesiąca musi być MM-yyyy.")]
        [Display(Name = "Miesiąc (MM-yyyy)")]
        public string Miesiac { get; set; }

        [Required(ErrorMessage = "Typ służby jest wymagany.")]
        [Display(Name = "Typ Służby")]
        public int IdSluzby { get; set; }
    }
}
