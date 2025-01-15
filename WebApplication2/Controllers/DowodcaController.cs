using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using WebApplication2.Models;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Dowodca")] // Dostęp tylko dla roli "Dowodca"
    public class DowodcaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IHostEnvironment _hostEnvironment;
        public DowodcaController(ApplicationDbContext context, IHostEnvironment hostEnvironment, IMemoryCache memoryCache)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _memoryCache = memoryCache;
        }

        // ----------------------------------
        // Panel główny Dowódcy
        // ----------------------------------
        public IActionResult DowodcaView()
        {
            return View();
        }

        // ----------------------------------
        // Lista Harmonogramów
        // ----------------------------------
        public IActionResult HarmonogramKC()
        {
            Console.WriteLine("Metoda HarmonogramKC została wywołana!");
            // Pobieramy aktualnie zalogowanego dowódcę
            var dowodcaId = User.Identity.Name; // Zmienna zależna od sposobu autentykacji

            // Rozdzielamy login na imię i nazwisko
            var imieNazwisko = dowodcaId.Split('.'); // Zakładamy, że login ma postać Imie.Nazwisko
            if (imieNazwisko.Length != 2)
            {
                return BadRequest("Niepoprawny format loginu.");
            }

            var imie = imieNazwisko[0]; // Imie
            var nazwisko = imieNazwisko[1]; // Nazwisko

            // Znajdź żołnierza w tabeli Zolnierze, który ma przypisane imię i nazwisko
            var zolnierz = _context.Zolnierze
                .FirstOrDefault(z => z.Imie == imie && z.Nazwisko == nazwisko); // Porównanie z imieniem i nazwiskiem

            // Sprawdzamy, czy żołnierz istnieje
            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza o tym imieniu i nazwisku.");
            }

            // Pobieramy ID pododdziału przypisane temu żołnierzowi
            var pododdzialId = zolnierz.ID_Pododdzialu;

            // Pobieramy harmonogramy tylko dla żołnierzy przypisanych do tego samego pododdziału,
            // wraz z informacjami o zastępcach
            var harmonogram = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Include(h => h.Zastepcy) // Dodane
                    .ThenInclude(z => z.ZolnierzZastepowanego) // Dodane
                .Where(h => h.Zolnierz.ID_Pododdzialu == pododdzialId) // Filtrowanie po pododdziale
                .OrderBy(h => h.Data) // Sortowanie według daty
                .ToList();

            return View(harmonogram);
        }



        // ----------------------------------
        // GET: Dodawanie Harmonogramu
        // ----------------------------------
        [HttpGet]
        public async Task<IActionResult> DodajHarmonogramKC()
        {
            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();

            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return View();
        }

        // ----------------------------------
        // POST: Dodawanie Harmonogramu
        // ----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajHarmonogramKC(Harmonogram harmonogram)
        {
            if (harmonogram.ID_Zolnierza != null)
            {
                // Pobierz żołnierza i służbę z bazy
                var zolnierz = await _context.Zolnierze.FirstOrDefaultAsync(z => z.ID_Zolnierza == harmonogram.ID_Zolnierza);
                var sluzba = await _context.Sluzby.FirstOrDefaultAsync(s => s.ID_Sluzby == harmonogram.ID_Sluzby);

                if (zolnierz != null && sluzba != null)
                {
                    // Budowanie zapytania SQL
                    string sqlQuery = "INSERT INTO SluzbaApp.Harmonogram_dane (ID_Zolnierza, ID_Sluzby, Data, Miesiac) " +
                                      "VALUES (@ID_Zolnierza, @ID_Sluzby, @Data, @Miesiac)";

                    // Użycie ExecuteSqlRawAsync do wykonania zapytania SQL
                    await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                        new MySqlParameter("@ID_Zolnierza", harmonogram.ID_Zolnierza),
                        new MySqlParameter("@ID_Sluzby", harmonogram.ID_Sluzby),
                        new MySqlParameter("@Data", harmonogram.Data),  // Zakładając, że Data jest typu DateTime
                        new MySqlParameter("@Miesiac", harmonogram.Miesiac));  // Miesiac to string

                    Console.WriteLine($"ID_Zolnierza: {harmonogram.ID_Zolnierza}");
                    Console.WriteLine($"ID_Sluzby: {harmonogram.ID_Sluzby}");
                    Console.WriteLine($"Data: {harmonogram.Data}");
                    Console.WriteLine($"Miesiac: {harmonogram.Miesiac}");

                    return RedirectToAction("HarmonogramKC");
                }
            }

            // W przypadku błędów, załaduj dane do ViewBag
            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();
            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return RedirectToAction("HarmonogramKC");
        }

        // ----------------------------------
        // Modyfikacja Punktów
        // ----------------------------------
        public IActionResult Punktacja()
        {
            // Pobieramy aktualnie zalogowanego dowódcę
            var dowodcaId = User.Identity.Name; // Zmienna zależna od sposobu autentykacji

            // Rozdzielamy login na imię i nazwisko
            var imieNazwisko = dowodcaId.Split('.'); // Zakładamy, że login ma postać Imie.Nazwisko
            if (imieNazwisko.Length != 2)
            {
                return BadRequest("Niepoprawny format loginu.");
            }

            var imie = imieNazwisko[0]; // Imię
            var nazwisko = imieNazwisko[1]; // Nazwisko

            // Znajdź żołnierza w tabeli Zolnierze, który ma przypisane imię i nazwisko
            var zolnierz = _context.Zolnierze
                .FirstOrDefault(z => z.Imie == imie && z.Nazwisko == nazwisko); // Porównanie z imieniem i nazwiskiem

            // Sprawdzamy, czy żołnierz istnieje
            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza o tym imieniu i nazwisku.");
            }

            // Pobieramy ID pododdziału przypisane temu żołnierzowi
            var pododdzialId = zolnierz.ID_Pododdzialu;

            // Pobieramy żołnierzy przypisanych do tego samego pododdziału
            var zolnierzeWPododdziale = _context.Zolnierze
                .Where(z => z.ID_Pododdzialu == pododdzialId) // Filtrowanie po pododdziale
                .OrderBy(z => z.Nazwisko) // Sortowanie według nazwiska
                .ToList();

            // Przekazujemy listę żołnierzy do widoku
            return View(zolnierzeWPododdziale);
        }


        [HttpPost]
        public IActionResult DodajPunkty(int ID_Zolnierza, int punkty)
        {
            var zolnierz = _context.Zolnierze.FirstOrDefault(z => z.ID_Zolnierza == ID_Zolnierza);
            if (zolnierz != null)
            {
                zolnierz.Punkty += punkty;
                _context.SaveChanges();
            }
            return RedirectToAction("Punktacja");
        }

        // ----------------------------------
        // Lista i Dodawanie Zwolnień
        // ----------------------------------
        public async Task<IActionResult> ListaZwolnien()
        {
            // Pobieramy aktualnie zalogowanego dowódcę
            var dowodcaId = User.Identity.Name; // Zmienna zależna od sposobu autentykacji

            // Rozdzielamy login na imię i nazwisko
            var imieNazwisko = dowodcaId.Split('.'); // Zakładamy, że login ma postać Imie.Nazwisko
            if (imieNazwisko.Length != 2)
            {
                return BadRequest("Niepoprawny format loginu.");
            }

            var imie = imieNazwisko[0]; // Imię
            var nazwisko = imieNazwisko[1]; // Nazwisko

            // Znajdź żołnierza w tabeli Zolnierze, który ma przypisane imię i nazwisko
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.Imie == imie && z.Nazwisko == nazwisko);

            // Sprawdzamy, czy żołnierz istnieje
            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza o tym imieniu i nazwisku.");
            }

            // Pobieramy ID pododdziału przypisane temu żołnierzowi
            var pododdzialId = zolnierz.ID_Pododdzialu;

            // Pobieramy zwolnienia tylko dla żołnierzy przypisanych do tego samego pododdziału
            var zwolnienia = await _context.Zwolnienia
                .Include(z => z.Zolnierz)
                .Where(z => z.Zolnierz.ID_Pododdzialu == pododdzialId) // Filtrowanie po pododdziale
                .ToListAsync();

            // Pobieramy listę żołnierzy w tym samym pododdziale
            ViewBag.Zolnierze = await _context.Zolnierze
                .Where(z => z.ID_Pododdzialu == pododdzialId) // Filtrowanie po pododdziale
                .ToListAsync();

            // Zwracamy widok z danymi
            return View(zwolnienia);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajZwolnienie(Zwolnienie zwolnienie)
        {
            if (zwolnienie.ID_Zolnierza != null)
            {
                var zolnierz = await _context.Zolnierze
                    .FirstOrDefaultAsync(z => z.ID_Zolnierza == zwolnienie.ID_Zolnierza);

                if (zolnierz != null)
                {
                    // Wykonujemy INSERT do bazy przez surowe zapytanie
                    await _context.Database.ExecuteSqlRawAsync(
                        @"INSERT INTO SluzbaApp.Zwolnienia_dane (ID_Zolnierza, Data_rozpoczecia_zwolnienia, Data_zakonczenia_zwolnienia)
                          VALUES (@ID_Zolnierza, @DataRozpoczecia, @DataZakonczenia)",
                        new MySqlParameter("@ID_Zolnierza", zwolnienie.ID_Zolnierza),
                        new MySqlParameter("@DataRozpoczecia", zwolnienie.DataRozpoczeciaZwolnienia),
                        new MySqlParameter("@DataZakonczenia", zwolnienie.DataZakonczeniaZwolnienia));

                    return RedirectToAction(nameof(ListaZwolnien));
                }
            }

            ViewData["Zolnierze"] = await _context.Zolnierze.ToListAsync();
            return View("ListaZwolnien", zwolnienie);
        }

        // =========================
        // PRZYDZIEL ZASTĘPCĘ
        // =========================

        // GET: /Dowodca/PrzydzielZastepce?idHarmonogram=XYZ
        [HttpGet]
        public IActionResult PrzydzielZastepce(int idHarmonogram)
        {
            // Znajdź dany wpis w Harmonogramie
            var harmonogramItem = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Include(h => h.Zastepcy) // Dodane
                    .ThenInclude(z => z.ZolnierzZastepowanego) // Dodane
                .FirstOrDefault(h => h.ID_Harmonogram == idHarmonogram);

            if (harmonogramItem == null)
                return NotFound("Nie znaleziono wpisu w harmonogramie.");

            // Wykluczamy obecnie przypisanego żołnierza
            var obecnyZolnierzId = harmonogramItem.ID_Zolnierza;
            var zolnierze = _context.Zolnierze
                .Where(z => z.ID_Zolnierza != obecnyZolnierzId)
                .ToList();

            ViewBag.HarmonogramItem = harmonogramItem;
            ViewBag.DostepniZolnierze = zolnierze;

            return View(); // przydzielzastepce.cshtml
        }




        // GET: /Dowodca/ListaPowiadomien
        // sortColumn może być np. "Zolnierz", "Tresc", "DataWyslania", "Status"
        // sortOrder "asc" lub "desc"
        public async Task<IActionResult> ListaPowiadomien(string sortColumn, string sortOrder)
        {
            // 1) Pobieramy aktualnie zalogowanego dowódcę
            var dowodcaId = User.Identity.Name; // Zmienna zależna od sposobu autentykacji

            // Rozdzielamy login na imię i nazwisko
            var imieNazwisko = dowodcaId.Split('.'); // Zakładamy, że login ma postać Imie.Nazwisko
            if (imieNazwisko.Length != 2)
            {
                return BadRequest("Niepoprawny format loginu.");
            }

            var imie = imieNazwisko[0]; // Imię
            var nazwisko = imieNazwisko[1]; // Nazwisko

            // Znajdź żołnierza w tabeli Zolnierze, który ma przypisane imię i nazwisko
            var zolnierz = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.Imie == imie && z.Nazwisko == nazwisko);

            if (zolnierz == null)
            {
                return NotFound("Nie znaleziono żołnierza o tym imieniu i nazwisku.");
            }

            // Pobieramy ID pododdziału przypisane temu żołnierzowi
            var pododdzialId = zolnierz.ID_Pododdzialu;

            // 2) Pobieramy wszystkie powiadomienia wraz z danymi żołnierza
            var query = _context.Powiadomienia
                .Include(p => p.Zolnierz)
                .Where(p => p.Zolnierz.ID_Pododdzialu == pododdzialId) // Filtrowanie po pododdziale
                .AsQueryable();

            // 3) Domyślne sortowanie: DataIGodzinaWyslania desc
            if (string.IsNullOrEmpty(sortColumn)) sortColumn = "Data";
            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "desc";

            // 4) Sortowanie wg. parametru
            query = sortColumn switch
            {
                "Zolnierz" => (sortOrder == "asc")
                    ? query.OrderBy(p => p.Zolnierz.Nazwisko)
                    : query.OrderByDescending(p => p.Zolnierz.Nazwisko),

                "Tresc" => (sortOrder == "asc")
                    ? query.OrderBy(p => p.TrescPowiadomienia)
                    : query.OrderByDescending(p => p.TrescPowiadomienia),

                "Status" => (sortOrder == "asc")
                    ? query.OrderBy(p => p.Status)
                    : query.OrderByDescending(p => p.Status),

                "Data" => (sortOrder == "asc")
                    ? query.OrderBy(p => p.DataIGodzinaWyslania)
                    : query.OrderByDescending(p => p.DataIGodzinaWyslania),

                _ => query.OrderByDescending(p => p.DataIGodzinaWyslania) // domyślne
            };

            // 5) Pobieramy z bazy
            var powiadomienia = await query.ToListAsync();

            // 6) Przekazujemy do widoku
            // By wiedzieć, jaka aktualnie kolumna i order
            ViewBag.CurrentSortColumn = sortColumn;
            ViewBag.CurrentSortOrder = sortOrder;

            return View(powiadomienia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrzydzielZastepce(int ID_Harmonogram, int ZastepcaId)
        {
            try
            {
                // Znajdź harmonogram wraz z zastępcami
                var harmonogramItem = await _context.Harmonogramy
                    .Include(h => h.Zolnierz)
                    .Include(h => h.Sluzba)
                    .Include(h => h.Zastepcy)
                        .ThenInclude(z => z.ZolnierzZastepowanego)
                    .FirstOrDefaultAsync(h => h.ID_Harmonogram == ID_Harmonogram);

                if (harmonogramItem == null)
                    return NotFound("Nie znaleziono wpisu w harmonogramie.");

                // Sprawdź, czy już istnieje zastępca
                bool hasSubstitute = harmonogramItem.Zastepcy != null && harmonogramItem.Zastepcy.Any();

                if (hasSubstitute)
                {
                    ModelState.AddModelError("", "Zastępca już został przydzielony.");

                    // Przygotuj dane do ponownego renderowania widoku z błędem
                    var obecnyZolnierzId = harmonogramItem.ID_Zolnierza;
                    var zolnierze = await _context.Zolnierze
                        .Where(z => z.ID_Zolnierza != obecnyZolnierzId)
                        .ToListAsync();

                    ViewBag.HarmonogramItem = harmonogramItem;
                    ViewBag.DostepniZolnierze = zolnierze;

                    return View(); // przydzielzastepce.cshtml
                }

                // Dodaj nowego zastępcę
                var nowyZastepca = new Zastepca
                {
                    ID_Harmonogram = ID_Harmonogram,
                    ID_Zolnierza_Zastepowanego = ZastepcaId,
                    DataPrzydzielenia = DateTime.Now
                };

                _context.Zastepcy.Add(nowyZastepca);
                await _context.SaveChangesAsync();

                // Dodaj powiadomienie do nowego zastępcy
                var nowePowiadomienie = new Powiadomienie
                {
                    ID_Zolnierza = ZastepcaId,
                    TrescPowiadomienia = $"Przydzielono Cię do służby w dniu {harmonogramItem.Data:yyyy-MM-dd}",
                    TypPowiadomienia = "Zastępstwo",
                    DataIGodzinaWyslania = DateTime.Now,
                    Status = "Wysłano"
                };
                _context.Powiadomienia.Add(nowePowiadomienie);
                await _context.SaveChangesAsync();

                // Przekierowanie do listy harmonogramu
                return RedirectToAction("HarmonogramKC");
            }
            catch (Exception ex)
            {
                // Logowanie błędu (konieczne jest dodanie ILogger do kontrolera)
                // _logger.LogError(ex, "Błąd podczas przydzielania zastępcy.");

                ModelState.AddModelError("", "Wystąpił błąd podczas przydzielania zastępcy.");

                // Ponownie załaduj dostępnych żołnierzy do widoku
                var harmonogramItem = await _context.Harmonogramy
                    .Include(h => h.Zolnierz)
                    .Include(h => h.Sluzba)
                    .Include(h => h.Zastepcy)
                        .ThenInclude(z => z.ZolnierzZastepowanego)
                    .FirstOrDefaultAsync(h => h.ID_Harmonogram == ID_Harmonogram);

                if (harmonogramItem == null)
                    return NotFound("Nie znaleziono wpisu w harmonogramie.");

                var obecnyZolnierzId = harmonogramItem.ID_Zolnierza;
                var zolnierze = await _context.Zolnierze
                    .Where(z => z.ID_Zolnierza != obecnyZolnierzId)
                    .ToListAsync();

                ViewBag.HarmonogramItem = harmonogramItem;
                ViewBag.DostepniZolnierze = zolnierze;

                return View(); // przydzielzastepce.cshtml
            }
        }

        public ActionResult Download()
        {
            try
            {
                List<Harmonogram> harmonogram;

                if (_memoryCache.TryGetValue("Harmonogram", out List<Harmonogram>? _harmonogram))
                {
                    harmonogram = _harmonogram;
                }
                else
                {
                    harmonogram = _context.Harmonogramy
                                                .Include(h => h.Zolnierz)  // Ładowanie powiązanych danych żołnierza
                                                .Include(h => h.Sluzba)    // Ładowanie powiązanych danych służby
                                                .OrderBy(h => h.Data)
                                                .ToList();
                }

                if (harmonogram == null || harmonogram.Count <= 0)
                {
                    throw new Exception("No data to parse.");
                }

                // Generate PDF
                using (var memoryStream = new MemoryStream())
                {
                    PdfWriter writer = new PdfWriter(memoryStream);
                    PdfDocument pdf = new PdfDocument(writer);
                    iText.Layout.Document document = new iText.Layout.Document(pdf);

                    var fontPath = Path.Combine(_hostEnvironment.ContentRootPath, "fonts/Roboto-Medium.ttf");
                    var regularFont = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);

                    // Add a title
                    document.Add(new Paragraph("Harmonogram Służb").SetFont(regularFont).SetFontSize(18).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    // Create a table with the same number of columns as the data
                    var table = new iText.Layout.Element.Table(5).SetHorizontalAlignment(HorizontalAlignment.CENTER);

                    string[] headers = { "ID", "Data", "Imie", "Nazwisko", "Służba" };
                    foreach (var header in headers)
                    {
                        table.AddHeaderCell(
                            new Cell().Add(new Paragraph(header).SimulateBold()) // Make header bold
                                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY) // Background color
                                .SetTextAlignment(TextAlignment.CENTER) // Center-align header text
                                .SetFont(regularFont)); //set bold font
                    }

                    // Add rows to the table
                    foreach (var row in harmonogram)
                    {
                        string[] data = [row.ID_Harmonogram.ToString(),
                        row.Data.ToString("yyyy-MM-dd"),
                        row.Zolnierz?.Imie,
                        row.Zolnierz?.Nazwisko,
                        row.Sluzba?.Rodzaj];

                        foreach (var dt in data)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(dt)).SetPadding(10).SetFont(regularFont));
                        }

                    }

                    document.Add(table);
                    document.Close();

                    // Return the PDF as a downloadable file
                    byte[] fileBytes = memoryStream.ToArray();
                    string fileName = "Harmonogram.pdf";
                    return File(fileBytes, "application/pdf", fileName);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }



        // ===============================================
        // 1) GET: Wybór służby, aby sprawdzić powiadomienia
        // ===============================================
        [HttpGet]
        public async Task<IActionResult> SprawdzPowiadomienia()
        {
            // Pobieramy listę służb, żeby Dowódca mógł wybrać z dropdown
            var sluzby = await _context.Sluzby.ToListAsync();

            // Można też posortować, albo wybrać te, które mają powiadomienia.
            // Dla uproszczenia pobieramy wszystkie.
            ViewBag.Sluzby = sluzby;

            // Zwracamy pusty model, czekając na wybór służby
            return View();
        }

        // ===============================================
        // 2) POST: Po wybraniu służby -> sprawdź status
        // ===============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SprawdzPowiadomienia(int ID_Sluzby)
        {
            // Znajdź wskazaną służbę
            var sluzba = await _context.Sluzby.FindAsync(ID_Sluzby);
            if (sluzba == null)
            {
                ModelState.AddModelError("", "Nie znaleziono wybranej służby.");
                // Przekazujemy listę służb ponownie, aby można było wybrać
                ViewBag.Sluzby = await _context.Sluzby.ToListAsync();
                return View();
            }

            // Teraz chcemy wybrać powiadomienia, które dotyczą tej służby.
            // Łączymy tabele: Powiadomienie -> Zolnierz -> Harmonogram -> Sluzba
            // Sposób 1: Z użyciem LINQ
            // Pseudokod:
            var query =
                from p in _context.Powiadomienia
                join z in _context.Zolnierze on p.ID_Zolnierza equals z.ID_Zolnierza
                join h in _context.Harmonogramy on z.ID_Zolnierza equals h.ID_Zolnierza
                where h.ID_Sluzby == ID_Sluzby
                select new { Powiadomienie = p, Zolnierz = z };

            var powiadomieniaSluzby = await query.ToListAsync();

            // Liczymy ile jest odebranych / nieodebranych
            // Zakładamy, że p.Status = "Odebrane" / "Wysłano" / "Nieodebrane" itd.
            int total = powiadomieniaSluzby.Count;
            int odebraneCount = powiadomieniaSluzby.Count(x => x.Powiadomienie.Status == "Odebrane");
            int nieodebraneCount = total - odebraneCount;

            // Lista nieodebranych
            var nieodebrani = powiadomieniaSluzby
                .Where(x => x.Powiadomienie.Status != "Odebrane")
                .Select(x => x.Zolnierz.Imie + " " + x.Zolnierz.Nazwisko)
                .ToList();

            // Przygotowujemy ViewModel z danymi do wyświetlenia
            var vm = new PowiadomieniaStatusViewModel
            {
                NazwaSluzby = sluzba.Rodzaj,
                LiczbaPowiadomien = total,
                LiczbaOdebranych = odebraneCount,
                LiczbaNieodebranych = nieodebraneCount,
                NieodebraniZolnierze = nieodebrani
            };

            return View("WynikPowiadomien", vm);
        }
    }

    // ========================================
    // ViewModel do wyświetlania wyników
    // ========================================
    public class PowiadomieniaStatusViewModel
    {
        public string NazwaSluzby { get; set; }
        public int LiczbaPowiadomien { get; set; }
        public int LiczbaOdebranych { get; set; }
        public int LiczbaNieodebranych { get; set; }
        public List<string> NieodebraniZolnierze { get; set; }
    }
}
