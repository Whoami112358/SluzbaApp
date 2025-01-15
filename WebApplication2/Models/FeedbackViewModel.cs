using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    // Model widoku dla zgłaszania feedbacku/sugestii
    public class FeedbackViewModel
    {
        // Jeśli nie wiążesz feedbacku z konkretnym wpisem w harmonogramie, to pole może pozostać opcjonalne
        public int? ID_Harmonogramu { get; set; }

        [Required(ErrorMessage = "Pole feedbacku nie może być puste.")]
        public string Tresc { get; set; }
    }
}
