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
using System.Diagnostics;

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
        // Panel główny Dowódcy   + WYSYŁANIE POWIADOMIEŃ DOTYCZĄCYCH SŁUŻB NA DZIEŃ DZISIEJSZY I NASTĘPNY
        // ----------------------------------
        public async Task<IActionResult> DowodcaView()
        {
            // Pobranie dzisiejszej daty
            var dzisiaj = DateTime.Today;
            var jutro = dzisiaj.AddDays(1);

            // Pobranie ID aktualnie zalogowanego dowódcy
            var dowodcaLogin = User.Identity.Name; // Zależne od implementacji autoryzacji
            var dowodca = await _context.Zolnierze
                .Include(z => z.Pododdzial) // Ładowanie powiązanego pododdziału
                .FirstOrDefaultAsync(z => z.LoginData.LoginName == dowodcaLogin);

            if (dowodca == null)
            {
                return Unauthorized("Nie znaleziono dowódcy.");
            }

            // Pobranie służb na dzisiaj i jutro
            var sluzbyDzisiaj = await _context.Harmonogramy
                .Include(h => h.Zolnierz)
                    .ThenInclude(z => z.Pododdzial) // Ładowanie pododdziału żołnierzy
                .Include(h => h.Sluzba)
                .Where(h => h.Data.Date == dzisiaj)
                .ToListAsync();

            var sluzbyJutro = await _context.Harmonogramy
                .Include(h => h.Zolnierz)
                    .ThenInclude(z => z.Pododdzial) // Ładowanie pododdziału żołnierzy
                .Include(h => h.Sluzba)
                .Where(h => h.Data.Date == jutro)
                .ToListAsync();

            // Tworzenie powiadomień
            var powiadomienia = new List<Powiadomienie>();

            if (sluzbyDzisiaj.Any())
            {
                var istniejePowiadomienieDzisiaj = await _context.Powiadomienia
                    .AnyAsync(p => p.ID_Zolnierza == dowodca.ID_Zolnierza &&
                                   p.TypPowiadomienia == "Informacja o służbach (dziś)");

                if (!istniejePowiadomienieDzisiaj)
                {
                    var trescDzisiaj = "Służby na dzisiaj:\n" +
                        string.Join("\n", sluzbyDzisiaj.Select(s =>
                            $"{s.Data:yyyy-MM-dd} - {s.Zolnierz.Imie} {s.Zolnierz.Nazwisko} ({s.Sluzba.Rodzaj}) -  {s.Zolnierz.Pododdzial?.Nazwa}"));

                    powiadomienia.Add(new Powiadomienie
                    {
                        ID_Zolnierza = dowodca.ID_Zolnierza,
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
                    .AnyAsync(p => p.ID_Zolnierza == dowodca.ID_Zolnierza &&
                                   p.TypPowiadomienia == "Informacja o służbach (jutro)");

                if (!istniejePowiadomienieJutro)
                {
                    var trescJutro = "Służby na jutro:\n" +
                        string.Join("\n", sluzbyJutro.Select(s =>
                            $"{s.Data:yyyy-MM-dd} - {s.Zolnierz.Imie} {s.Zolnierz.Nazwisko} ({s.Sluzba.Rodzaj}) -  {s.Zolnierz.Pododdzial?.Nazwa}"));

                    powiadomienia.Add(new Powiadomienie
                    {
                        ID_Zolnierza = dowodca.ID_Zolnierza,
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
           

            // Przekieruj z powrotem do listy żołnierzy/ekranu punktacji
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

            // 2) Pobieramy wszystkie powiadomienia przypisane wyłącznie do dowódcy
            var query = _context.Powiadomienia
                .Include(p => p.Zolnierz)
                .Where(p => p.ID_Zolnierza == zolnierz.ID_Zolnierza) // Filtrowanie tylko powiadomień dowódcy
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




        // GET: /Dowodca/PrzydzielAutomatycznie
        [HttpGet]
        public async Task<IActionResult> PrzydzielAutomatycznie()
        {
            // Lista kryteriów – ustawiamy statycznie
            List<string> kryteria = new List<string> { "Punkty", "Wiek", "Stopien" };
            ViewBag.Kryteria = new SelectList(kryteria);

            // Pobieramy listę typów służb z bazy danych
            var sluzby = await _context.Sluzby.ToListAsync();
            ViewBag.Sluzby = new SelectList(sluzby, "ID_Sluzby", "Rodzaj");

            // Ustawiamy pusty model, który zostanie powiązany z widokiem
            return View(new PrzydzielAutomatycznieViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrzydzielAutomatycznie(PrzydzielAutomatycznieViewModel model, string submitAction)
        {
            // Ładujemy listy dla SelectList – aby w razie błędów formularz był poprawnie wypełniony
            List<string> kryteria = new List<string> { "Punkty", "Wiek", "Stopien" };
            ViewBag.Kryteria = new SelectList(kryteria);
            var sluzby = await _context.Sluzby.ToListAsync();
            ViewBag.Sluzby = new SelectList(sluzby, "ID_Sluzby", "Rodzaj");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Parsowanie miesiąca: zakładamy format "MM-yyyy" (np. "01-2025")
            if (!DateTime.TryParseExact($"01-{model.Miesiac}", "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime miesiacData))
            {
                ModelState.AddModelError("", "Podano niepoprawny format miesiąca. Użyj formatu MM-yyyy.");
                return View(model);
            }

            // Konwersja numeru miesiąca na polską nazwę miesiąca (np. "Styczeń")
            string monthName = System.Globalization.CultureInfo
                .GetCultureInfo("pl-PL")
                .DateTimeFormat.GetMonthName(miesiacData.Month);

            // Upewnij się, że pierwsza litera jest wielka:
            if (!string.IsNullOrEmpty(monthName))
            {
                monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);
            }

            // Obsługa przycisku "wyswietl"
            if (submitAction == "wyswietl")
            {
                // Pobierz harmonogram dla danego miesiąca (używając nazwy miesiąca) i wybranego typu służby
                var harmonogram = await _context.Harmonogramy
                    .Include(h => h.Zolnierz)
                    .Include(h => h.Sluzba)
                    .Where(h => h.Miesiac.ToLower() == monthName.ToLower() && h.ID_Sluzby == model.IdSluzby)
                    .OrderBy(h => h.Data)
                    .ToListAsync();

                // Przekaż harmonogram do widoku przez ViewBag
                ViewBag.Harmonogram = harmonogram;
                return View(model);
            }
            // Obsługa przycisku "przydziel"
            else if (submitAction == "przydziel")
            {
                // 1) Pobierz istniejący harmonogram dla wybranego miesiąca (przy użyciu nazwy miesiąca) i typu służby
                var harmonogramMiesiac = await _context.Harmonogramy
                    .Where(h => h.Miesiac.ToLower() == monthName.ToLower() && h.ID_Sluzby == model.IdSluzby)
                    .ToListAsync();

                int dniWMiesiacu = DateTime.DaysInMonth(miesiacData.Year, miesiacData.Month);

                // Lista wszystkich dni miesiąca
                List<DateTime> wszystkieDni = new List<DateTime>();
                for (int d = 1; d <= dniWMiesiacu; d++)
                {
                    wszystkieDni.Add(new DateTime(miesiacData.Year, miesiacData.Month, d));
                }

                // Lista dni, dla których nie ma przypisanej służby
                var dniBezSluzby = wszystkieDni
                    .Where(d => !harmonogramMiesiac.Any(h => h.Data.Date == d.Date))
                    .ToList();

                // 2) Pobierz wszystkich żołnierzy uporządkowanych według wybranego kryterium
                IQueryable<Zolnierz> query = _context.Zolnierze;
                switch (model.WybraneKryterium)
                {
                    case "Punkty":
                        query = query.OrderBy(z => z.Punkty);
                        break;
                    case "Wiek":
                        query = query.OrderBy(z => z.Wiek);
                        break;
                    case "Stopien":
                        query = query.OrderBy(z => z.Stopien);
                        break;
                    default:
                        break;
                }
                var zolnierze = await query.ToListAsync();

                // Słownik przechowujący liczbę przydzielonych służb dla danego żołnierza
                Dictionary<int, int> sluzbyNaMiesiacu = new Dictionary<int, int>();

                // Ładujemy również wpisy harmonogramu (dla sprawdzenia, czy w dniu poprzednim żołnierz miał służbę)
                var harmonogramMiesiacWgZolnierzy = await _context.Harmonogramy
                    .Where(h => h.Miesiac.ToLower() == monthName.ToLower())
                    .ToListAsync();

                bool CzyMialDzienPrzed(int idZolnierza, DateTime data)
                {
                    DateTime poprzedniDzien = data.AddDays(-1);
                    return harmonogramMiesiacWgZolnierzy.Any(h => h.ID_Zolnierza == idZolnierza && h.Data.Date == poprzedniDzien.Date);
                }

                // 3) Przeprowadź automatyczny przydział dla każdego wolnego dnia
                foreach (var dzien in dniBezSluzby)
                {
                    var wybranyZolnierz = zolnierze.FirstOrDefault(z =>
                    {
                        int iloscSluzb = sluzbyNaMiesiacu.ContainsKey(z.ID_Zolnierza)
                            ? sluzbyNaMiesiacu[z.ID_Zolnierza]
                            : harmonogramMiesiacWgZolnierzy.Count(h => h.ID_Zolnierza == z.ID_Zolnierza);

                        if (iloscSluzb >= 5)
                            return false;
                        if (CzyMialDzienPrzed(z.ID_Zolnierza, dzien))
                            return false;
                        return true;
                    });

                    if (wybranyZolnierz != null)
                    {
                        string sqlQuery = "INSERT INTO SluzbaApp.Harmonogram_dane (ID_Zolnierza, ID_Sluzby, Data, Miesiac) " +
                                          "VALUES (@ID_Zolnierza, @ID_Sluzby, @Data, @Miesiac)";

                        await _context.Database.ExecuteSqlRawAsync(sqlQuery,
                            new MySqlParameter("@ID_Zolnierza", wybranyZolnierz.ID_Zolnierza),
                            new MySqlParameter("@ID_Sluzby", model.IdSluzby),
                            new MySqlParameter("@Data", dzien),
                            // Zapisujemy nazwę miesiąca (np. "Styczeń")
                            new MySqlParameter("@Miesiac", monthName));

                        if (sluzbyNaMiesiacu.ContainsKey(wybranyZolnierz.ID_Zolnierza))
                            sluzbyNaMiesiacu[wybranyZolnierz.ID_Zolnierza]++;
                        else
                            sluzbyNaMiesiacu[wybranyZolnierz.ID_Zolnierza] = harmonogramMiesiacWgZolnierzy.Count(h => h.ID_Zolnierza == wybranyZolnierz.ID_Zolnierza) + 1;

                        // Aktualizujemy lokalną listę harmonogramu, by dalsze sprawdzenie (np. CzyMialDzienPrzed) uwzględniało nowy wpis
                        harmonogramMiesiacWgZolnierzy.Add(new Harmonogram
                        {
                            ID_Zolnierza = wybranyZolnierz.ID_Zolnierza,
                            ID_Sluzby = model.IdSluzby,
                            Data = dzien,
                            Miesiac = monthName
                        });
                    }
                }

                // Po przydzieleniu przekierowujemy do widoku harmonogramu (np. akcji HarmonogramKC)
                return RedirectToAction("HarmonogramKC");
            }
            else
            {
                return View(model);
            }

        }

        // =======================================
        // GET: /Dowodca/ZarzadzajPriorytetami
        // =======================================
        [HttpGet]
        public async Task<IActionResult> ZarzadzajPriorytetami()
        {
            // 1. Pobierz dane zalogowanego dowódcy (login w formacie Imie.Nazwisko)
            var dowodcaLogin = User.Identity.Name;
            if (string.IsNullOrEmpty(dowodcaLogin))
            {
                return Unauthorized("Brak zalogowanego dowódcy.");
            }

            var imieNazwisko = dowodcaLogin.Split('.');
            if (imieNazwisko.Length != 2)
            {
                return BadRequest("Niepoprawny format loginu (oczekiwano Imie.Nazwisko).");
            }

            var imie = imieNazwisko[0];
            var nazwisko = imieNazwisko[1];

            // 2. Znajdź dowódcę w tabeli Zolnierze
            var dowodca = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.Imie == imie && z.Nazwisko == nazwisko);

            if (dowodca == null)
            {
                return NotFound("Nie znaleziono dowódcy o podanym loginie.");
            }

            // 3. Pobierz wszystkie służby (Sluzba_dane)
            var wszystkieSluzby = await _context.Sluzby.ToListAsync();

            // 4. Pobierz wszystkie priorytety dla aktualnego dowódcy
            var priorytetyDlaDowodcy = await _context.Priorytety
                .Where(p => p.ID_Zolnierza == dowodca.ID_Zolnierza)
                .ToListAsync();

            // 5. Jeśli dla którejś służby nie ma ustawionego priorytetu, dodaj go z domyślną wartością 1
            bool isNewRecordAdded = false;
            foreach (var sluzba in wszystkieSluzby)
            {
                if (!priorytetyDlaDowodcy.Any(p => p.ID_Sluzby == sluzba.ID_Sluzby))
                {
                    var nowyPriorytet = new Priorytet
                    {
                        ID_Zolnierza = dowodca.ID_Zolnierza,
                        ID_Sluzby = sluzba.ID_Sluzby,
                        PriorytetValue = 1 // domyślny priorytet
                    };
                    _context.Priorytety.Add(nowyPriorytet);
                    priorytetyDlaDowodcy.Add(nowyPriorytet);
                    isNewRecordAdded = true;
                }
            }

            if (isNewRecordAdded)
            {
                // Zapisujemy do bazy dopiero po dodaniu wszystkich brakujących rekordów
                await _context.SaveChangesAsync();
            }

            // 6. Ponownie pobierz priorytety, ale już z danymi o służbie
            var priorytetyZSluzbami = await _context.Priorytety
                .Where(p => p.ID_Zolnierza == dowodca.ID_Zolnierza)
                .Include(p => p.Sluzba)
                .ToListAsync();

            // 7. Zwróć widok z listą priorytetów
            return View(priorytetyZSluzbami);
        }

        // =======================================
        // POST: /Dowodca/ZarzadzajPriorytetami
        // =======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZarzadzajPriorytetami(List<Priorytet> priorytety)
        {
            // 1. Ponownie pobierz dane zalogowanego dowódcy
            var dowodcaLogin = User.Identity.Name;
            if (string.IsNullOrEmpty(dowodcaLogin))
            {
                return Unauthorized("Brak zalogowanego dowódcy.");
            }

            var imieNazwisko = dowodcaLogin.Split('.');
            if (imieNazwisko.Length != 2)
            {
                return BadRequest("Niepoprawny format loginu (oczekiwano Imie.Nazwisko).");
            }

            var imie = imieNazwisko[0];
            var nazwisko = imieNazwisko[1];

            var dowodca = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.Imie == imie && z.Nazwisko == nazwisko);

            if (dowodca == null)
            {
                return NotFound("Nie znaleziono dowódcy o podanym loginie.");
            }

            // 2. Walidacja podstawowa
            if (!ModelState.IsValid)
            {
                // Jeśli jest błąd walidacji, zwracamy widok z błędami
                return View(priorytety);
            }

            // 3. Przetwarzanie priorytetów
            foreach (var priorytet in priorytety)
            {
                // Upewniamy się, że priorytet należy do aktualnie zalogowanego dowódcy
                if (priorytet.ID_Zolnierza != dowodca.ID_Zolnierza)
                {
                    // Ewentualnie można zignorować, a można też zgłosić błąd
                    ModelState.AddModelError("", "Nieautoryzowana zmiana priorytetu.");
                    continue;
                }

                // Znajdź istniejący rekord w bazie
                var existingPriorytet = await _context.Priorytety
                    .FirstOrDefaultAsync(p => p.ID_Priorytetu == priorytet.ID_Priorytetu &&
                                              p.ID_Zolnierza == dowodca.ID_Zolnierza);

                if (existingPriorytet != null)
                {
                    // Zaktualizuj wartość priorytetu
                    existingPriorytet.PriorytetValue = priorytet.PriorytetValue;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(priorytety);
            }

            // 4. Zapisanie zmian w bazie i przekierowanie do HarmonogramKC
            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Priorytety zostały zaktualizowane.";
            }
            catch (Exception ex)
            {
                // Obsługa ewentualnych błędów DB
                ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji priorytetów: " + ex.Message);
                return View(priorytety);
            }

            // 5. Po zapisaniu: redirect do HarmonogramKC
            return RedirectToAction("HarmonogramKC", "Dowodca");
        }

        [HttpGet]
        public IActionResult AnalizaDostepnosci()
        {
            // 1) Generujemy listę miesięcy i lat do wyboru (np. bieżący rok i kolejny?)
            var months = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var years = new List<int> { DateTime.Now.Year, DateTime.Now.Year + 1 };

            ViewBag.Months = months; // do <select>
            ViewBag.Years = years;

            // 2) Możemy ustawić domyślny wybór: aktualny miesiąc/rok
            ViewBag.SelectedMonth = DateTime.Now.Month;
            ViewBag.SelectedYear = DateTime.Now.Year;

            // Na razie brak eventów w widoku → user wybierze i wciśnie "Pokaż"
            return View(); // AnalizaDostepnosci.cshtml (formularz i/lub kalendarz)
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalizaDostepnosci(int month, int year)
        {
            // 1) Znajdź dowódcę
            var login = User.Identity.Name; // np. "Jan.Kowalski"
            var parts = login.Split('.');
            if (parts.Length != 2)
            {
                ModelState.AddModelError("", "Niepoprawny format loginu.");
                return View(); // Wróci do formularza
            }
            string imie = parts[0];
            string nazwisko = parts[1];

            var dowodca = await _context.Zolnierze
                .FirstOrDefaultAsync(z => z.Imie == imie && z.Nazwisko == nazwisko);

            if (dowodca == null)
            {
                ModelState.AddModelError("", "Nie znaleziono dowódcy w bazie.");
                return View();
            }

            // 2) Wyznacz zakres (pierwszy i ostatni dzień wybranego miesiąca)
            // Sprawdzamy poprawność month/year w razie czego
            if (month < 1 || month > 12 || year < 2000 || year > 2100)
            {
                ModelState.AddModelError("", "Niepoprawny zakres miesiąca/roku.");
                return View();
            }

            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // 3) Pobieramy wszystkich żołnierzy z pododdziału dowódcy
            var zolnierzePododdzial = await _context.Zolnierze
                .Where(z => z.ID_Pododdzialu == dowodca.ID_Pododdzialu)
                .ToListAsync();

            // 4) Pobieramy Harmonogram w tym miesiącu
            var harmonogramOkres = await _context.Harmonogramy
                .Where(h => h.Data >= firstDay && h.Data <= lastDay)
                .ToListAsync();

            // 5) Pobieramy Urlopy i Zwolnienia
            var urlopyOkres = await _context.Urlopy
                .Where(u => u.DataRozpoczecia <= lastDay && u.DataZakonczenia >= firstDay)
                .ToListAsync();
            var zwolnieniaOkres = await _context.Zwolnienia
                .Where(z => z.DataRozpoczeciaZwolnienia <= lastDay
                         && z.DataZakonczeniaZwolnienia >= firstDay)
                .ToListAsync();

            // 6) Budujemy dayMap [yyyy-MM-dd -> DayDetails]
            int totalDays = (lastDay - firstDay).Days + 1;
            var dayMap = new Dictionary<string, DayDetails>();
            for (int i = 0; i < totalDays; i++)
            {
                var d = firstDay.AddDays(i);
                string key = d.ToString("yyyy-MM-dd");
                dayMap[key] = new DayDetails
                {
                    Dostepni = new List<string>(),
                    Niedostepni = new List<string>()
                };
            }

            // ...
            // 7) Dla każdej doby i każdego żołnierza → sprawdzamy dostępność
            for (int i = 0; i < totalDays; i++)
            {
                var d = firstDay.AddDays(i);
                string dateKey = d.ToString("yyyy-MM-dd");

                foreach (var z in zolnierzePododdzial)
                {
                    string powod = SprawdzNiedostepnosc(z.ID_Zolnierza, d,
                        harmonogramOkres, urlopyOkres, zwolnieniaOkres);

                    // Budujemy zapis w formacie: "Stopien Imie Nazwisko"
                    var opisZolnierza = $"{z.Stopien} {z.Imie} {z.Nazwisko}";

                    if (powod == null)
                    {
                        // Żołnierz dostępny
                        dayMap[dateKey].Dostepni.Add(opisZolnierza);
                    }
                    else
                    {
                        // Żołnierz niedostępny + powód
                        dayMap[dateKey].Niedostepni.Add($"{opisZolnierza} ({powod})");
                    }
                }
            }


            // 8) Budujemy eventy (po jednym na każdy dzień)
            var events = new List<object>();
            for (int i = 0; i < totalDays; i++)
            {
                var d = firstDay.AddDays(i);
                string dateKey = d.ToString("yyyy-MM-dd");
                var details = dayMap[dateKey];
                int countDost = details.Dostepni.Count;
                int countNied = details.Niedostepni.Count;

                string title = $"Dost. {countDost}, Nied. {countNied}";
                // Kolory – ciemna czcionka, by lepiej widać
                string bgColor = "#f9c74f";  // np. żółtawy
                string txtColor = "#000000"; // czarny

                events.Add(new
                {
                    title = title,
                    start = dateKey,
                    allDay = true,
                    color = bgColor,
                    textColor = txtColor
                });
            }

            // Konwersja do JSON
            ViewBag.EventsJson = System.Text.Json.JsonSerializer.Serialize(events);

            // Szczegóły do modala
            var dayInfo = dayMap.ToDictionary(
                x => x.Key,
                x => new {
                    Dostepni = x.Value.Dostepni,
                    Niedostepni = x.Value.Niedostepni
                }
            );
            ViewBag.DayInfoJson = System.Text.Json.JsonSerializer.Serialize(dayInfo);

            // 9) Zapisujemy do ViewBag, by widok wiedział, jaki to miesiąc
            ViewBag.CurrentMonth = month;
            ViewBag.CurrentYear = year;

            // Ponownie listy do <select>, by mogły się wyświetlić
            var months = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var years = new List<int> { DateTime.Now.Year, DateTime.Now.Year + 1 };
            ViewBag.Months = months;
            ViewBag.Years = years;

            // Zwracamy ten sam widok AnalizaDostepnosci.cshtml
            return View();
        }

        // Pomocnicza metoda do sprawdzenia powodu niedostępności
        private string SprawdzNiedostepnosc(int idZolnierza, DateTime dzien,
            List<Harmonogram> harmList,
            List<Urlop> urlopList,
            List<Zwolnienie> zwolList)
        {
            // 1) W służbie tego dnia?
            bool wSluzbie = harmList.Any(h => h.ID_Zolnierza == idZolnierza
                                           && h.Data.Date == dzien.Date);
            if (wSluzbie)
                return "służba";

            // 2) Dzień po służbie => odpoczynek
            var wczoraj = dzien.AddDays(-1);
            bool wczorajSluzba = harmList.Any(h => h.ID_Zolnierza == idZolnierza
                                                && h.Data.Date == wczoraj.Date);
            if (wczorajSluzba)
                return "odpoczynek";

            // 3) Urlop?
            bool wUrlopie = urlopList.Any(u => u.ID_Zolnierza == idZolnierza
                && u.DataRozpoczecia <= dzien && u.DataZakonczenia >= dzien);
            if (wUrlopie)
                return "urlop";

            // 4) Zwolnienie?
            bool wZwolnieniu = zwolList.Any(z => z.ID_Zolnierza == idZolnierza
                && z.DataRozpoczeciaZwolnienia <= dzien
                && z.DataZakonczeniaZwolnienia >= dzien);
            if (wZwolnieniu)
                return "zwolnienie";

            // Jeśli nic – jest dostępny
            return null;
        }

        // Pomocnicza klasa do przechowywania listy dostępnych/niedostępnych
        private class DayDetails
        {
            public List<string> Dostepni { get; set; }
            public List<string> Niedostepni { get; set; }
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