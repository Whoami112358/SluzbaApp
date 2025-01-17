using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web; // do ewentualnego HttpUtility.UrlEncode
using System.Collections.Generic;
using WebApplication2.Models;
using System.ComponentModel.DataAnnotations;
using MySqlConnector;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Schedule/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }

            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Harmonogramy zalogowanego żołnierza
            var harmonogramy = await _context.Harmonogramy
                .Where(h => h.ID_Zolnierza == idZolnierza)
                .Include(h => h.Sluzba)
                .ToListAsync();

            // Jeśli brak, inicjuj pustą listę
            if (harmonogramy == null)
            {
                harmonogramy = new List<Harmonogram>();
            }

            return View(harmonogramy);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZmienHarmonogram(int idHarmonogram, Harmonogram nowyHarmonogram)
        {
            // Pobierz istniejący harmonogram z bazy
            var harmonogram = await _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Zolnierz.Pododdzial) // Dowódca związany z pododdziałem
                .FirstOrDefaultAsync(h => h.ID_Harmonogram == idHarmonogram);

            if (harmonogram == null)
            {
                return NotFound("Nie znaleziono harmonogramu.");
            }

            // Sprawdź, czy nastąpiły zmiany w harmonogramie
            bool czyZmieniono = harmonogram.Data != nowyHarmonogram.Data ||
                                harmonogram.ID_Sluzby != nowyHarmonogram.ID_Sluzby;

            if (czyZmieniono)
            {
                // Aktualizuj dane harmonogramu
                harmonogram.Data = nowyHarmonogram.Data;
                harmonogram.ID_Sluzby = nowyHarmonogram.ID_Sluzby;

                // Pobierz dowódcę pododdziału
                var dowodca = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Pododdzialu == harmonogram.Zolnierz.ID_Pododdzialu && z.Stopien == "Dowódca");

                if (dowodca != null)
                {
                    // Tworzenie powiadomienia dla dowódcy
                    var powiadomienie = new Powiadomienie
                    {
                        ID_Zolnierza = dowodca.ID_Zolnierza,
                        TrescPowiadomienia = $"Zmiana w harmonogramie: Żołnierz {harmonogram.Zolnierz.Imie} {harmonogram.Zolnierz.Nazwisko} pełni służbę dnia {nowyHarmonogram.Data:yyyy-MM-dd}.",
                        TypPowiadomienia = "Zmiana harmonogramu",
                        DataIGodzinaWyslania = DateTime.Now,
                        Status = "Wysłano"
                    };

                    _context.Powiadomienia.Add(powiadomienie);
                }
            }

            // Zapisz zmiany w bazie danych
            _context.Harmonogramy.Update(harmonogram);
            await _context.SaveChangesAsync();

            return RedirectToAction("ListaHarmonogramow"); // Przekierowanie do widoku z listą harmonogramów
        }










        // POST: /Schedule/ZlozWniosekZmianyTerminu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZlozWniosekZmianyTerminu(WniosekZmianyTerminuViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Błąd walidacji - wracamy do widoku
                return RedirectToAction("Index");
            }

            // Znalezienie wpisu w harmonogramie
            var harmItem = await _context.Harmonogramy
                .Include(h => h.Sluzba)
                .FirstOrDefaultAsync(h => h.ID_Harmonogram == model.ID_Harmonogramu);

            if (harmItem == null)
            {
                ModelState.AddModelError("", "Nie znaleziono służby w harmonogramie.");
                return RedirectToAction("Index");
            }

            // Pobranie ID żołnierza z jego roli (claim)
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }
            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Pobranie danych żołnierza z tabeli Zolnierze
            var zolnierz = await _context.Zolnierze
                .Where(z => z.ID_Zolnierza == idZolnierza)
                .Select(z => new { z.Imie, z.Nazwisko, z.Stopien, z.ID_Pododdzialu })
                .FirstOrDefaultAsync();

            if (zolnierz == null)
            {
                ModelState.AddModelError("", "Nie znaleziono danych żołnierza.");
                return RedirectToAction("Index");
            }

            // Wyszukiwanie dowódcy (kapitana) w tym samym pododdziale
            var dowodca = await _context.Zolnierze
                .Where(z => z.ID_Pododdzialu == zolnierz.ID_Pododdzialu && z.Stopien == "kpt")
                .FirstOrDefaultAsync();

            if (dowodca == null)
            {
                ModelState.AddModelError("", "Nie znaleziono dowódcy o stopniu kpt.");
                return RedirectToAction("Index");
            }

            int IDdowodca = dowodca.ID_Zolnierza;

            // Konwersja daty z formularza (ProponowanaData)
            DateTime proponowanaData;
            if (!DateTime.TryParse(model.ProponowanaData, out proponowanaData))
            {
                ModelState.AddModelError("", "Nieprawidłowy format daty.");
                return RedirectToAction("Index");
            }

            // Tworzenie treści powiadomienia z dodaniem danych żołnierza
            string tresc = $"Proszę o zmianę terminu służby z dnia {harmItem.Data.ToString("yyyy-MM-dd")} " +
                           $"z powodu {model.Uzasadnienie} na dzień {proponowanaData.ToString("yyyy-MM-dd")} " +
                           $"/ {zolnierz.Stopien} {zolnierz.Imie} {zolnierz.Nazwisko}";

            // Zapisanie powiadomienia w bazie danych
            string sqlQuery = @"INSERT INTO SluzbaApp.Powiadomienia_dane 
(ID_Zolnierza, Tresc_powiadomienia, Typ_powiadomienia, Data_i_godzina_wyslania, Status)
VALUES (@ID_Zolnierza, @Tresc, @Typ, @DataIGodzina, @Status)";

            await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                new MySqlParameter("@ID_Zolnierza", IDdowodca),
                new MySqlParameter("@Tresc", tresc),
                new MySqlParameter("@Typ", "Wniosek"),
                new MySqlParameter("@DataIGodzina", DateTime.Now),
                new MySqlParameter("@Status", "Wysłano"));

            // Powiadomienie zostało wysłane, przekierowanie na stronę główną lub wynikową
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // POST: /Schedule/ZglosFeedback
        // Metoda przetwarzająca zgłoszenie feedbacku/sugestii od żołnierza.
        // Wzorowana jest na rozwiązaniu dla wniosku o zmianę terminu służby.
        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZglosFeedback(FeedbackViewModel model)
        {
            // Sprawdzenie walidacji modelu
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            // Pobranie ID zalogowanego żołnierza z claimów
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }
            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Pobranie danych żołnierza (np. imie, nazwisko, stopień, ID_Pododdzialu)
            var zolnierz = await _context.Zolnierze
                .Where(z => z.ID_Zolnierza == idZolnierza)
                .Select(z => new { z.Imie, z.Nazwisko, z.Stopien, z.ID_Pododdzialu })
                .FirstOrDefaultAsync();

            if (zolnierz == null)
            {
                ModelState.AddModelError("", "Nie znaleziono danych żołnierza.");
                return RedirectToAction("Index");
            }

            // Wyszukanie dowódcy w tym samym pododdziale (przyjmujemy, że stopień dowódcy to "kpt")
            var dowodca = await _context.Zolnierze
                .Where(z => z.ID_Pododdzialu == zolnierz.ID_Pododdzialu && z.Stopien == "kpt")
                .FirstOrDefaultAsync();

            if (dowodca == null)
            {
                ModelState.AddModelError("", "Nie znaleziono dowódcy o stopniu kpt.");
                return RedirectToAction("Index");
            }

            int IDdowodca = dowodca.ID_Zolnierza;

            // Utworzenie treści powiadomienia z feedbackiem
            string tresc = $"Feedback od {zolnierz.Stopien} {zolnierz.Imie} {zolnierz.Nazwisko}: {model.Tresc}";

            // Przygotowanie zapytania SQL do wstawienia powiadomienia
            string sqlQuery = @"INSERT INTO SluzbaApp.Powiadomienia_dane 
(ID_Zolnierza, Tresc_powiadomienia, Typ_powiadomienia, Data_i_godzina_wyslania, Status)
VALUES (@ID_Zolnierza, @Tresc, @Typ, @DataIGodzina, @Status)";

            // Wstawienie powiadomienia dla dowódcy
            await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                new MySqlParameter("@ID_Zolnierza", IDdowodca),
                new MySqlParameter("@Tresc", tresc),
                new MySqlParameter("@Typ", "Feedback"),
                new MySqlParameter("@DataIGodzina", DateTime.Now),
                new MySqlParameter("@Status", "Wysłano"));

         
            // Ustawienie komunikatu potwierdzającego (opcjonalnie możesz użyć TempData aby wyświetlić komunikat po przekierowaniu)
            TempData["FeedbackMessage"] = "Feedback został zapisany. Dziękujemy za Twoje sugestie.";

            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // POST: /Schedule/ZglosKonflikt
        // Przetwarza zgłoszenie konfliktu służbowego.
        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZglosKonflikt(KonfliktViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            // Pobranie ID zalogowanego żołnierza
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }
            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Pobranie danych żołnierza
            var zolnierz = await _context.Zolnierze
                .Where(z => z.ID_Zolnierza == idZolnierza)
                .Select(z => new { z.Imie, z.Nazwisko, z.Stopien, z.ID_Pododdzialu })
                .FirstOrDefaultAsync();
            if (zolnierz == null)
            {
                ModelState.AddModelError("", "Nie znaleziono danych żołnierza.");
                return RedirectToAction("Index");
            }

            // Wyszukanie dowódcy (stopień "kpt") w tym samym pododdziale
            var dowodca = await _context.Zolnierze
                .Where(z => z.ID_Pododdzialu == zolnierz.ID_Pododdzialu && z.Stopien == "kpt")
                .FirstOrDefaultAsync();
            if (dowodca == null)
            {
                ModelState.AddModelError("", "Nie znaleziono dowódcy o stopniu kpt.");
                return RedirectToAction("Index");
            }

            // (Opcjonalnie:) Możesz wyszukać też oficera dyżurnego, jeśli wiadomo gdzie kierować takie zgłoszenie.

            // Utworzenie treści powiadomienia z konfliktem – format przykładowy
            string tresc = $"Konflikt służbowy zgłoszony przez {zolnierz.Stopien} {zolnierz.Imie} {zolnierz.Nazwisko}:" +
                           $" Dzień: {model.Dzien.ToString("yyyy-MM-dd")}, " +
                           $"Od: {model.OdGodziny}, Do: {model.DoGodziny}. " +
                           $"Powód: {model.PowodKonfliktu}";

            // Przygotowanie zapytania SQL do zapisania powiadomienia
            string sqlQuery = @"INSERT INTO SluzbaApp.Powiadomienia_dane 
(ID_Zolnierza, Tresc_powiadomienia, Typ_powiadomienia, Data_i_godzina_wyslania, Status)
VALUES (@ID_Zolnierza, @Tresc, @Typ, @DataIGodzina, @Status)";

            // Wstawienie powiadomienia dla dowódcy
            await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                new MySqlParameter("@ID_Zolnierza", dowodca.ID_Zolnierza),
                new MySqlParameter("@Tresc", tresc),
                new MySqlParameter("@Typ", "Konflikt"),
                new MySqlParameter("@DataIGodzina", DateTime.Now),
                new MySqlParameter("@Status", "Wysłano"));

            // (Opcjonalnie:) Wstawienie powiadomienia dla innych, np. oficera dyżurnego

            TempData["FeedbackMessage"] = "Konflikt został zgłoszony. Powiadomienie zostało wysłane w odpowiednie miejsce.";
            return RedirectToAction("Index");
        }


        // ------------------------------------------------------------------
        // POST: /Schedule/ZglosPriorytet
        // Przetwarza ustawienie priorytetu dla trzech rodzajów służby jednocześnie.
        // Jeśli rekord już istnieje (dla danego ID_Zolnierza i ID_Sluzby), zostanie nadpisany.
        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZglosPriorytet(PriorytetMultipleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            // Pobranie ID zalogowanego żołnierza
            var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
            if (idZolnierzaClaim == null)
            {
                return NotFound("Nie znaleziono żołnierza.");
            }
            var idZolnierza = int.Parse(idZolnierzaClaim);

            // Instrukcja INSERT ... ON DUPLICATE KEY UPDATE – dla nadpisania rekordu, jeżeli kombinacja (ID_Zolnierza, ID_Sluzby) już istnieje.
            string sqlQuery = @"
        INSERT INTO SluzbaApp.Priorytety_dane (ID_Zolnierza, ID_Sluzby, Priorytet)
        VALUES (@ID_Zolnierza, @ID_Sluzby, @Priorytet)
        ON DUPLICATE KEY UPDATE Priorytet = VALUES(Priorytet)";

            // Dla Warty (przyjmujemy, że ID_Sluzby = 1)
            await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                new MySqlParameter("@ID_Zolnierza", idZolnierza),
                new MySqlParameter("@ID_Sluzby", 1),
                new MySqlParameter("@Priorytet", model.PriorytetWarta));

            // Dla Patrolu (przyjmujemy, że ID_Sluzby = 2)
            await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                new MySqlParameter("@ID_Zolnierza", idZolnierza),
                new MySqlParameter("@ID_Sluzby", 2),
                new MySqlParameter("@Priorytet", model.PriorytetPatrol));

            // Dla Pododdziału (przyjmujemy, że ID_Sluzby = 3)
            await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                new MySqlParameter("@ID_Zolnierza", idZolnierza),
                new MySqlParameter("@ID_Sluzby", 3),
                new MySqlParameter("@Priorytet", model.PriorytetPododdzial));

            TempData["FeedbackMessage"] = "Priorytet służby został zapisany.";
            return RedirectToAction("Index");
        }





        // ------------------------------------------------------------
        // GET: /Schedule/AddReminder?idHarmonogram=XYZ
        // Wyświetla formularz do ustawienia szczegółów przypomnienia
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> AddReminder(int idHarmonogram)
        {
            // Wyszukujemy wpis w harmonogramie
            var harmItem = await _context.Harmonogramy
                .Include(h => h.Sluzba)
                .FirstOrDefaultAsync(h => h.ID_Harmonogram == idHarmonogram);

            if (harmItem == null)
            {
                return NotFound("Nie znaleziono służby w harmonogramie.");
            }

            // Domyślnie w formularzu możemy wstawić np. tytuł = "Służba: <rodzaj>"
            // Data/godzina to harmItem.Data (w tym przykładzie brak godziny w modelu, więc np. 00:00)
            // Ewentualnie można rozbudować Harmonogram o pole Godzina i tu je odczytywać.

            ViewBag.HarmItem = harmItem;
            // Przykładowy model widoku:
            var reminderVm = new AddReminderViewModel
            {
                ID_Harmonogram = harmItem.ID_Harmonogram,
                Tytul = $"Służba: {(harmItem.Sluzba != null ? harmItem.Sluzba.Rodzaj : "brak")}",
                DataSluzby = harmItem.Data.Date,  // tutaj wstawiamy samą datę
                GodzinaSluzby = new TimeSpan(0, 0, 0), // brak w modelu, więc domyślnie 00:00
                OffsetMinutes = 60, // domyślne przypomnienie 1h przed
                Notatki = ""
            };

            return View(reminderVm); // Wyświetlimy widok AddReminder.cshtml
        }

        [HttpGet]
        public IActionResult DownloadReport()
        {
            try
            {
                // Pobranie ID żołnierza z claimów
                var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
                if (idZolnierzaClaim == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                var idZolnierza = int.Parse(idZolnierzaClaim);

                // Wyznaczenie dat granicznych (6 miesięcy wstecz i dziś)
                DateTime sixMonthsAgo = DateTime.Now.AddMonths(-6);
                DateTime today = DateTime.Today;

                // Pobranie służb z tego zakresu
                var harmonogram = _context.Harmonogramy
                    .Where(h => h.ID_Zolnierza == idZolnierza
                             && h.Data >= sixMonthsAgo
                             && h.Data <= today)
                    .Include(h => h.Sluzba)
                    .OrderBy(h => h.Data)
                    .ToList();

                if (harmonogram == null || harmonogram.Count == 0)
                {
                    return NotFound("Brak danych do wygenerowania raportu za ostatnie 6 miesięcy.");
                }

                // Generowanie pliku PDF
                using (var memoryStream = new MemoryStream())
                {
                    PdfWriter writer = new PdfWriter(memoryStream);
                    PdfDocument pdf = new PdfDocument(writer);
                    using (var document = new iText.Layout.Document(pdf))
                    {
                        // Tytuł dokumentu
                        document.Add(new Paragraph("Raport Służb - Ostatnie 6 miesięcy")
                            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                            .SetFontSize(16)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginBottom(20));

                        // Tworzenie tabeli (3 kolumny)
                        Table table = new Table(3).UseAllAvailableWidth();
                        table.AddHeaderCell("Data").SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                        table.AddHeaderCell("Rodzaj Służby").SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                        table.AddHeaderCell("Miejsce").SetBackgroundColor(ColorConstants.LIGHT_GRAY);

                        // Wypełnianie danych w tabeli
                        foreach (var item in harmonogram)
                        {
                            table.AddCell(item.Data.ToString("yyyy-MM-dd"));
                            table.AddCell(item.Sluzba?.Rodzaj ?? "Brak");
                            table.AddCell(item.Sluzba?.MiejscePelnieniaSluzby ?? "Brak");
                        }

                        document.Add(table);
                    }

                    // Zwracanie pliku PDF
                    return File(memoryStream.ToArray(), "application/pdf", "Raport_Sluzb.pdf");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Wystąpił błąd podczas generowania raportu: {ex.Message}");
            }
        }



        // ------------------------------------------------------------
        // POST: /Schedule/AddReminder
        // Odbiera dane formularza, tworzy link do Google Kalendarza
        // Wyświetla link na ekranie lub od razu redirect
        // ------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddReminder(AddReminderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Błąd walidacji - wracamy do widoku
                return View(model);
            }

            // Znajdź wpis w harmonogramie, głównie by potwierdzić istnienie i np. zweryfikować ID żołnierza
            var harmItem = _context.Harmonogramy
                .Include(h => h.Sluzba)
                .FirstOrDefault(h => h.ID_Harmonogram == model.ID_Harmonogram);

            if (harmItem == null)
            {
                ModelState.AddModelError("", "Nie znaleziono harmonogramu w bazie.");
                return View(model);
            }

            // Obliczmy datę/godzinę startu i końca
            // Zakładamy, że służba trwa np. 8 godzin (lub cokolwiek innego).
            // Można to pobierać z innej kolumny, jeśli istnieje w bazie.
            DateTime startDateTime = model.DataSluzby.Date + model.GodzinaSluzby;
            DateTime endDateTime = startDateTime.AddHours(8);

            // Format do Google Calendar: 
            // "YYYYMMDDTHHMMSS" (local time)
            string startStr = startDateTime.ToString("yyyyMMddTHHmmss");
            string endStr = endDateTime.ToString("yyyyMMddTHHmmss");

            // Tytuł, opis
            string title = model.Tytul ?? $"Służba {harmItem.ID_Harmonogram}";
            string details = model.Notatki ?? "";

            // Google Calendar link (localtime):
            // https://calendar.google.com/calendar/render?action=TEMPLATE&text=TITLE&dates=YYYYMMDDTHHMMSS/YYYYMMDDTHHMMSS&details=NOTES
            string googleCalendarUrl = $"https://calendar.google.com/calendar/render?action=TEMPLATE" +
                $"&text={Uri.EscapeDataString(title)}" +
                $"&dates={startStr}/{endStr}" +
                $"&details={Uri.EscapeDataString(details)}";

            // Przekazujemy link do widoku potwierdzającego
            ViewBag.GoogleLink = googleCalendarUrl;
            ViewBag.Title = "Link do dodania przypomnienia";

            return View("AddReminderResult");
        }
    }

    // =================================================
    //   ViewModel do formularza przypomnienia
    // =================================================
    public class AddReminderViewModel
    {
        public int ID_Harmonogram { get; set; }

        // Data/godzina służby
        [Required]
        public DateTime DataSluzby { get; set; }

        [Required]
        public TimeSpan GodzinaSluzby { get; set; }

        // Tytuł i notatki w kalendarzu
        [Required]
        public string Tytul { get; set; }
        public string Notatki { get; set; }

        // Offset w minutach (np. 60 = 1h przed)
        [Range(0, 720)]
        public int OffsetMinutes { get; set; }
    }
}
