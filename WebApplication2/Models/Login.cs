using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Login_dane")] // Określenie, że ten model odnosi się do tabeli Login_dane w bazie
    public class Login
    {
        [Key]
        [Column("ID_Loginu")] // Określenie kolumny, która przechowuje unikalny identyfikator loginu
        public int ID_Loginu { get; set; } // Unikalny identyfikator loginu (klucz główny)

        [Column("ID_Zolnierza")] // Określenie kolumny, która jest kluczem obcym do tabeli Zolnierz_dane
        public int ID_Zolnierza { get; set; }

        [Column("Login")] // Określenie kolumny, która przechowuje login użytkownika
        public string LoginName { get; set; }  // Login użytkownika

        [Column("Haslo")] // Określenie kolumny, która przechowuje hasło użytkownika (zahashowane)
        public string Haslo { get; set; }      // Hasło użytkownika

        [Column("Email")] // Określenie kolumny, która przechowuje email użytkownika
        public string Email { get; set; }      // Email użytkownika

        // Relacja 1:1 z Zolnierz
        // Login jest powiązany z jednym żołnierzem
        public virtual Zolnierz Zolnierz { get; set; }
    }
}
