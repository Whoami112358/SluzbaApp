using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class KonfliktViewModel
    {
        [Required(ErrorMessage = "Wybierz dzień konfliktu.")]
        public DateTime Dzien { get; set; }

        [Required(ErrorMessage = "Podaj godzinę rozpoczęcia konfliktu.")]
        public TimeSpan OdGodziny { get; set; }

        [Required(ErrorMessage = "Podaj godzinę zakończenia konfliktu.")]
        public TimeSpan DoGodziny { get; set; }

        [Required(ErrorMessage = "Podaj powód konfliktu.")]
        public string PowodKonfliktu { get; set; }
    }
}
