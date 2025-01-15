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

            // Jeśli brak, zainicjuj pustą listę
            if (harmonogramy == null)
            {
                harmonogramy = new List<Harmonogram>();
            }

            return View(harmonogramy);
        }

        // POST: /Schedule/ZlozWniosekZmianyTerminu
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
            // "YYYYMMDDTHHMMSSZ" (UTC) lub bez 'Z' jeśli local time
            // Prostota: localtime "YYYYMMDDTHHMMSS"
            string startStr = startDateTime.ToString("yyyyMMddTHHmmss");
            string endStr = endDateTime.ToString("yyyyMMddTHHmmss");

            // Tytuł, opis
            string title = model.Tytul ?? $"Służba {harmItem.ID_Harmonogram}";
            string details = model.Notatki ?? "";

            // Można dodać offset np. w parametrach notatek ("Przypomnienie 1h przed") 
            // Google Calendar link (bez 'Z' - localtime):
            // https://calendar.google.com/calendar/render?action=TEMPLATE&text=TITLE&dates=20230901T080000/20230901T160000&details=NOTES
            string googleCalendarUrl = $"https://calendar.google.com/calendar/render?action=TEMPLATE" +
                $"&text={Uri.EscapeDataString(title)}" +
                $"&dates={startStr}/{endStr}" +
                $"&details={Uri.EscapeDataString(details)}";

            // W docelowym scenariuszu można generować ICS lub link do Outlook
            // Tutaj zwracamy link do Google

            // Przekazujemy link do widoku potwierdzającego
            ViewBag.GoogleLink = googleCalendarUrl;
            ViewBag.Title = "Link do dodania przypomnienia";

            return View("AddReminderResult");
        }

        public IActionResult DownloadReport()
        {
            try
            {
                // Pobranie ID żołnierza z sesji (np. z claimów)
                var idZolnierzaClaim = User.FindFirst("ID_Zolnierza")?.Value;
                if (idZolnierzaClaim == null)
                {
                    return NotFound("Nie znaleziono żołnierza.");
                }

                var idZolnierza = int.Parse(idZolnierzaClaim);

                // Pobranie harmonogramu dla ostatnich 6 miesięcy
                DateTime sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var harmonogram = _context.Harmonogramy
                    .Where(h => h.ID_Zolnierza == idZolnierza && h.Data >= sixMonthsAgo)
                    .Include(h => h.Sluzba)
                    .OrderBy(h => h.Data)
                    .ToList();

                if (harmonogram == null || harmonogram.Count == 0)
                {
                    return NotFound("Brak danych do wygenerowania raportu.");
                }

                // Generowanie pliku PDF
                using (var memoryStream = new MemoryStream())
                {
                    PdfWriter writer = new PdfWriter(memoryStream);
                    PdfDocument pdf = new PdfDocument(writer);
                    iText.Layout.Document document = new iText.Layout.Document(pdf);

                    // Tytuł dokumentu
                    document.Add(new Paragraph("Raport Sluzb - Ostatnie 6 miesiecy")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(16)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(20));

                    // Tworzenie tabeli
                    Table table = new Table(3).UseAllAvailableWidth();
                    table.AddHeaderCell("Data").SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                    table.AddHeaderCell("Rodzaj Sluzby").SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                    table.AddHeaderCell("Miejsce").SetBackgroundColor(ColorConstants.LIGHT_GRAY);

                    // Wypełnianie danych w tabeli
                    foreach (var item in harmonogram)
                    {
                        table.AddCell(item.Data.ToString("yyyy-MM-dd"));
                        table.AddCell(item.Sluzba?.Rodzaj ?? "Brak");
                        table.AddCell(item.Sluzba?.MiejscePelnieniaSluzby ?? "Brak");
                    }

                    document.Add(table);
                    document.Close();

                    // Zwracanie pliku PDF
                    return File(memoryStream.ToArray(), "application/pdf", "Raport_Sluzb.pdf");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Wystąpił błąd podczas generowania raportu: {ex.Message}");
            }
        }


    }

    // =================================================
    //   ViewModel do formularza
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
