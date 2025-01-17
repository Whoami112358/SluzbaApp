using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using MySqlConnector;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties; 
using iText.IO.Font;
using iText.Kernel.Font;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace WebApplication2.Controllers
{  // Zabezpieczenie kontrolera, aby dostęp miały tylko osoby z rolą "Officer"
    public class DyzurnyController : Controller
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ApplicationDbContext _context;
        IMemoryCache _memoryCache;
        public DyzurnyController(ApplicationDbContext context, IHostEnvironment hostEnvironment, IMemoryCache memoryCache)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _memoryCache = memoryCache;
        }
        public async Task<IActionResult> DyzurnyView()
        {
            // Pobranie dzisiejszej daty
            var dzisiaj = DateTime.Today;
            var jutro = dzisiaj.AddDays(1);

            // Pobranie ID aktualnie zalogowanego oficera dyżurnego
            var dyzurnyLogin = User.Identity.Name; // Zależne od implementacji autoryzacji
            var dyzurny = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.LoginData.LoginName == dyzurnyLogin);

            if (dyzurny == null)
            {
                return Unauthorized("Nie znaleziono oficera dyżurnego.");
            }

            // Pobranie służb na dzisiaj i jutro
            var sluzbyDzisiaj = await _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Where(h => h.Data.Date == dzisiaj)
                .ToListAsync();

            var sluzbyJutro = await _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Where(h => h.Data.Date == jutro)
                .ToListAsync();

            // Tworzenie powiadomień
            var powiadomienia = new List<Powiadomienie>();

            if (sluzbyDzisiaj.Any())
            {
                var istniejePowiadomienieDzisiaj = await _context.Powiadomienia
                    .AnyAsync(p => p.ID_Zolnierza == dyzurny.ID_Zolnierza &&
                                   p.TypPowiadomienia == "Informacja o służbach (dziś)");

                if (!istniejePowiadomienieDzisiaj)
                {
                    var trescDzisiaj = "Służby na dzisiaj:\n" +
                        string.Join("\n", sluzbyDzisiaj.Select(s =>
                            $"{s.Data:yyyy-MM-dd} - {s.Zolnierz.Imie} {s.Zolnierz.Nazwisko} ({s.Sluzba.Rodzaj})"));

                    powiadomienia.Add(new Powiadomienie
                    {
                        ID_Zolnierza = dyzurny.ID_Zolnierza,
                        TrescPowiadomienia = trescDzisiaj,
                        TypPowiadomienia = "Informacja o służbach (dziś)",
                        DataIGodzinaWyslania = DateTime.Now,
                        Status = "Wysłano"
                    });
                }
            }

            if (sluzbyJutro.Any())
            {
                var istniejePowiadomienieJutro = await _context.Powiadomienia
                    .AnyAsync(p => p.ID_Zolnierza == dyzurny.ID_Zolnierza &&
                                   p.TypPowiadomienia == "Informacja o służbach (jutro)");

                if (!istniejePowiadomienieJutro)
                {
                    var trescJutro = "Służby na jutro:\n" +
                        string.Join("\n", sluzbyJutro.Select(s =>
                            $"{s.Data:yyyy-MM-dd} - {s.Zolnierz.Imie} {s.Zolnierz.Nazwisko} ({s.Sluzba.Rodzaj})"));

                    powiadomienia.Add(new Powiadomienie
                    {
                        ID_Zolnierza = dyzurny.ID_Zolnierza,
                        TrescPowiadomienia = trescJutro,
                        TypPowiadomienia = "Informacja o służbach (jutro)",
                        DataIGodzinaWyslania = DateTime.Now,
                        Status = "Wysłano"
                    });
                }
            }

            // Zapisanie powiadomień w bazie danych
            if (powiadomienia.Any())
            {
                _context.Powiadomienia.AddRange(powiadomienia);
                await _context.SaveChangesAsync();
            }

            return View();
        }
        public IActionResult Punktacja(string sortBy, bool isDescending = false)
        {
            // Pobranie danych żołnierzy
            var zolnierze = _context.Zolnierze.AsQueryable();

            // Obsługa sortowania
            zolnierze = sortBy switch
            {
                "Imie" => isDescending ? zolnierze.OrderByDescending(z => z.Imie) : zolnierze.OrderBy(z => z.Imie),
                "Nazwisko" => isDescending ? zolnierze.OrderByDescending(z => z.Nazwisko) : zolnierze.OrderBy(z => z.Nazwisko),
                "Punkty" => isDescending ? zolnierze.OrderByDescending(z => z.Punkty) : zolnierze.OrderBy(z => z.Punkty),
                _ => zolnierze.OrderBy(z => z.Imie) // Domyślne sortowanie po imieniu
            };

            // Przekazanie posortowanych danych do widoku
            return View(zolnierze.ToList());
        }

        public IActionResult ListaZolnierzy(string sortBy, bool isDescending = false)
        {
            // Pobranie danych z bazy
            var zolnierze = _context.Zolnierze.AsQueryable();

            // Obsługa sortowania
            zolnierze = sortBy switch
            {
                "Imie" => isDescending ? zolnierze.OrderByDescending(z => z.Imie) : zolnierze.OrderBy(z => z.Imie),
                "Nazwisko" => isDescending ? zolnierze.OrderByDescending(z => z.Nazwisko) : zolnierze.OrderBy(z => z.Nazwisko),
                "Punkty" => isDescending ? zolnierze.OrderByDescending(z => z.Punkty) : zolnierze.OrderBy(z => z.Punkty),
                _ => zolnierze.OrderBy(z => z.Imie) // Domyślne sortowanie po imieniu
            };

            // Przekazanie danych do widoku
            return View(zolnierze.ToList());
        }
 


        public IActionResult HarmonogramKC(string sortBy, bool isDescending = false)
        {
            var harmonogram = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Include(h => h.Zastepcy) // Dodano do wczytania kolekcji Zastepcy
                .AsQueryable();

            // Sortowanie
            harmonogram = sortBy switch
            {
                "Data" => isDescending ? harmonogram.OrderByDescending(h => h.Data) : harmonogram.OrderBy(h => h.Data),
                "Imie" => isDescending ? harmonogram.OrderByDescending(h => h.Zolnierz.Imie) : harmonogram.OrderBy(h => h.Zolnierz.Imie),
                "Nazwisko" => isDescending ? harmonogram.OrderByDescending(h => h.Zolnierz.Nazwisko) : harmonogram.OrderBy(h => h.Zolnierz.Nazwisko),
                _ => harmonogram.OrderBy(h => h.Data)
            };

            // Bezpieczne tworzenie słownika zastępców
            var replacedSoldiers = harmonogram
                .Where(h => h.Zastepcy != null && h.Zastepcy.Any()) // Upewnij się, że kolekcja Zastepcy nie jest pusta
                .ToDictionary(
                    h => h.ID_Harmonogram,
                    h => h.Zastepcy.FirstOrDefault()?.ID_Zolnierza_Zastepowanego ?? 0 // Bezpieczne pobieranie wartości
                );

            var zolnierzeDb = _context.Zolnierze.ToList();

            ViewBag.ReplacedSoldiers = replacedSoldiers;
            ViewBag.ZolnierzeDb = zolnierzeDb;

            return View(harmonogram.ToList());
        }





        // GET: /Dyzurny/ListaPowiadomien
        // sortColumn może być np. "Zolnierz", "Tresc", "DataWyslania", "Status"
        // sortOrder "asc" lub "desc"
        public async Task<IActionResult> ListaPowiadomien(string sortColumn, string sortOrder)
        {
            // 1) Ustawienie ID Oficera Dyżurnego na sztywno
            const int dyzurnyId = 26;

            // 2) Pobieramy powiadomienia przypisane tylko do Oficera Dyżurnego (ID_Zolnierza = 0)
            var query = _context.Powiadomienia
                .Include(p => p.Zolnierz) // Dołączenie informacji o żołnierzu
                .Where(p => p.ID_Zolnierza == dyzurnyId) // Filtracja dla Oficera Dyżurnego
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
            // Informacje o aktualnie sortowanej kolumnie i porządku
            ViewBag.CurrentSortColumn = sortColumn;
            ViewBag.CurrentSortOrder = sortOrder;

            return View(powiadomienia);
        }
        public IActionResult HarmonogramSorted(string sortBy = "Data", bool isDescending = false)
        {
            var harmonogram = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .AsQueryable();

            // Sortowanie
            harmonogram = sortBy switch
            {
                "Data" => isDescending ? harmonogram.OrderByDescending(h => h.Data) : harmonogram.OrderBy(h => h.Data),
                "Imie" => isDescending
                    ? harmonogram.OrderByDescending(h => h.Zolnierz.Imie)
                    : harmonogram.OrderBy(h => h.Zolnierz.Imie),
                "Nazwisko" => isDescending
                    ? harmonogram.OrderByDescending(h => h.Zolnierz.Nazwisko)
                    : harmonogram.OrderBy(h => h.Zolnierz.Nazwisko),
                _ => harmonogram.OrderBy(h => h.Data) // Domyślne sortowanie
            };

            return View("HarmonogramKC", harmonogram.ToList());
        }



        public IActionResult ListaTygodniowa(string sortBy = "Data", bool isDescending = false)
        {
            // Pobranie aktualnej daty
            var currentDate = DateTime.Now;

            // Obliczenie daty początkowej i końcowej bieżącego tygodnia
            var startOfWeek = currentDate.Date.AddDays(-(int)currentDate.DayOfWeek + 1); // Poniedziałek
            var endOfWeek = startOfWeek.AddDays(6); // Niedziela

            // Filtrowanie danych harmonogramu na bieżący tydzień
            var harmonogram = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Where(h => h.Data >= startOfWeek && h.Data <= endOfWeek)
                .AsQueryable();

            // Sortowanie
            harmonogram = sortBy switch
            {
                "Data" => isDescending ? harmonogram.OrderByDescending(h => h.Data) : harmonogram.OrderBy(h => h.Data),
                "Imie" => isDescending ? harmonogram.OrderByDescending(h => h.Zolnierz.Imie) : harmonogram.OrderBy(h => h.Zolnierz.Imie),
                "Nazwisko" => isDescending ? harmonogram.OrderByDescending(h => h.Zolnierz.Nazwisko) : harmonogram.OrderBy(h => h.Zolnierz.Nazwisko),
                _ => harmonogram.OrderBy(h => h.Data) // Domyślne sortowanie po dacie
            };

            // Przekazanie danych do widoku
            return View(harmonogram.ToList());
        }



        /*
        // Akcja POST do dodawania punktów
        [HttpPost]
        public IActionResult DodajPunkty(int ID_Zolnierza, int punkty)
        {
            // Znajdź żołnierza w bazie danych
            var zolnierz = _context.Zolnierze.FirstOrDefault(z => z.ID_Zolnierza == ID_Zolnierza);
            if (zolnierz != null)
            {
                // Dodaj punkty do żołnierza
                zolnierz.Punkty += punkty;

                // Zapisz zmiany w bazie danych
                _context.SaveChanges();
            }

            // Po zaktualizowaniu danych przekieruj z powrotem do listy żołnierzy
            return RedirectToAction("Punktacja");
        }

        
        // Akcja POST do usuwania punktów
        [HttpPost]
        public IActionResult UsunPunkty(int ID_Zolnierza, int punkty)
        {
            // Logowanie początku akcji
            Debug.WriteLine($"UsunPunkty wywołane dla ID_Zolnierza: {ID_Zolnierza}, punkty: {punkty}");

            // Znajdź żołnierza w bazie danych
            var zolnierz = _context.Zolnierze.FirstOrDefault(z => z.ID_Zolnierza == ID_Zolnierza);
            if (zolnierz != null)
            {
                // Sprawdź, czy punkty do usunięcia są większe niż obecne punkty
                if (zolnierz.Punkty >= punkty)
                {
                    // Odejmij punkty od żołnierza
                    zolnierz.Punkty -= punkty;
                }
                else
                {
                    // Dodanie logiki do obsługi błędu, np. przekierowanie z komunikatem
                    TempData["Error"] = "Nie można usunąć więcej punktów niż aktualnie posiadane.";
                    return RedirectToAction("Punktacja");
                }

                // Zapisz zmiany w bazie danych
                _context.SaveChanges();
            }

            // Po zaktualizowaniu danych przekieruj z powrotem do listy żołnierzy
            return RedirectToAction("Punktacja");
        } */


        // Akcja POST do aktualizacji punktów (dodawanie/usuwanie)
        [HttpPost]
        public IActionResult ZaktualizujPunkty(int ID_Zolnierza, int punkty)
        {
            // Znajdź żołnierza w bazie danych
            var zolnierz = _context.Zolnierze.FirstOrDefault(z => z.ID_Zolnierza == ID_Zolnierza);
            if (zolnierz == null)
            {
                TempData["Error"] = "Żołnierz o podanym identyfikatorze nie został znaleziony.";
                return RedirectToAction("Punktacja");
            }

            // Jeśli punkty są ujemne - próbujemy je odjąć
            if (punkty < 0)
            {
                int punktyDoUsuniecia = Math.Abs(punkty);
                // Sprawdź, czy żołnierz ma wystarczającą liczbę punktów
                if (zolnierz.Punkty >= punktyDoUsuniecia)
                {
                    zolnierz.Punkty -= punktyDoUsuniecia;
                }
                else
                {
                    TempData["Error"] = "Nie można usunąć więcej punktów niż aktualnie posiadane.";
                    return RedirectToAction("Punktacja");
                }
            }
            else // W przeciwnym przypadku dodaj punkty
            {
                zolnierz.Punkty += punkty;
            }
            var powiadomienie = new Powiadomienie
            {
                ID_Zolnierza = ID_Zolnierza,
                TrescPowiadomienia = $"Twoja punktacja została zmieniona o {punkty}. Aktualna liczba punktów: {zolnierz.Punkty}.",
                TypPowiadomienia = "Zmiana punktacji",
                DataIGodzinaWyslania = DateTime.Now,
                Status = "Wysłano"
            };
            _context.Powiadomienia.Add(powiadomienie);
            _context.SaveChanges();

            // Zapisz zmiany w bazie danych
            _context.SaveChanges();

            // Przekieruj z powrotem do listy żołnierzy/ekranu punktacji
            return RedirectToAction("Punktacja");
        }


       

        // GET: /Admin/AddSchedule
        [HttpGet]
        public async Task<IActionResult> DodajHarmonogramKC()
        {
            // Użycie await, aby poczekać na wyniki zapytań
            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();

            // Przekazanie danych do widoku
            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return View();
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
            catch (Exception e) {
                return null;
            }
        }

        // POST: /Admin/AddSchedule
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


        public IActionResult Lista()
        {
            // Pobranie aktualnej daty
            var currentDate = DateTime.Now;
            // Obliczanie daty początkowej i końcowej bieżącego tygodnia
            var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            // Pobranie harmonogramów, które mają daty w bieżącym tygodniu
            var harmonogramy = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .Where(h => h.Data >= startOfWeek && h.Data < endOfWeek) // Filtrowanie po dacie
                .ToList();

            return View(harmonogramy);
        }

        // =========================
        // PRZYDZIEL ZASTĘPCĘ
        // =========================

        // GET: /Dyzurny/PrzydzielZastepce?idHarmonogram=XYZ
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
    }
}
