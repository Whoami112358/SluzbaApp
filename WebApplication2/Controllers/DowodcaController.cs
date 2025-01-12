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

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Dowodca")] // Dostęp tylko dla roli "Dowodca"
    public class DowodcaController : Controller
    {
        // Słownik w pamięci: ID_Harmonogram -> ID starego żołnierza
        // Używany do przekreślania w widoku poprzedniego przypisania
        private static Dictionary<int, int> replacedSoldiers = new Dictionary<int, int>();

        private readonly ApplicationDbContext _context;

        public DowodcaController(ApplicationDbContext context)
        {
            _context = context;
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
            var harmonogram = _context.Harmonogramy
                .Include(h => h.Zolnierz)
                .Include(h => h.Sluzba)
                .OrderBy(h => h.Data)
                .ToList();

            // Przekazujemy słownik do widoku,
            // aby wyświetlić starych żołnierzy przekreślonych
            ViewBag.ReplacedSoldiers = replacedSoldiers;
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
            if (ModelState.IsValid)
            {
                _context.Harmonogramy.Add(harmonogram);
                await _context.SaveChangesAsync();

                return RedirectToAction("HarmonogramKC");
            }

            var zolnierze = await _context.Zolnierze.ToListAsync();
            var sluzby = await _context.Sluzby.ToListAsync();
            ViewBag.Zolnierze = zolnierze;
            ViewBag.Sluzby = sluzby;

            return View(harmonogram);
        }

        // ----------------------------------
        // Modyfikacja Punktów
        // ----------------------------------
        public IActionResult Punktacja()
        {
            var zolnierze = _context.Zolnierze.ToList();
            return View(zolnierze);
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
            var zwolnienia = await _context.Zwolnienia
                .Include(z => z.Zolnierz)
                .ToListAsync();

            ViewBag.Zolnierze = await _context.Zolnierze.ToListAsync();
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
            // 1) Pobieramy wszystkie powiadomienia wraz z danymi żołnierza
            var query = _context.Powiadomienia
                .Include(p => p.Zolnierz)
                .AsQueryable();

            // 2) Domyślne sortowanie: DataIGodzinaWyslania desc
            if (string.IsNullOrEmpty(sortColumn)) sortColumn = "Data";
            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "desc";

            // 3) Sortowanie wg. parametru
            // Używamy switch, by sortować różne kolumny
            // (lub można użyć dynamicznej biblioteki do sortowania)
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

            // 4) Pobieramy z bazy
            var powiadomienia = await query.ToListAsync();

            // 5) Przekazujemy do widoku
            // By wiedzieć, jaka aktualnie kolumna i order
            ViewBag.CurrentSortColumn = sortColumn;
            ViewBag.CurrentSortOrder = sortOrder;

            return View(powiadomienia);
        }
        // POST: /Dowodca/PrzydzielZastepce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrzydzielZastepce(int ID_Harmonogram, int ZastepcaId)
        {
            // Znajdujemy oryginalny Harmonogram
            var harmonogramItem = _context.Harmonogramy
                .FirstOrDefault(h => h.ID_Harmonogram == ID_Harmonogram);

            if (harmonogramItem == null)
                return NotFound("Nie znaleziono wpisu w harmonogramie.");

            // Zapamiętujemy starego żołnierza, by przekreślić w widoku
            int staryZolnierzId = harmonogramItem.ID_Zolnierza;
            if (!replacedSoldiers.ContainsKey(ID_Harmonogram))
                replacedSoldiers.Add(ID_Harmonogram, staryZolnierzId);
            else
                replacedSoldiers[ID_Harmonogram] = staryZolnierzId;

            // Aktualizujemy w bazie: nowy żołnierz w Harmonogram_dane
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE SluzbaApp.Harmonogram_dane
                  SET ID_Zolnierza = @ZastepcaId
                  WHERE ID_Harmonogram = @HarmId",
                new MySqlParameter("@ZastepcaId", ZastepcaId),
                new MySqlParameter("@HarmId", ID_Harmonogram));

            // Dodaj wpis w Powiadomienia_dane
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO SluzbaApp.Powiadomienia_dane
                  (ID_Zolnierza, Tresc_powiadomienia, Typ_powiadomienia, Data_i_godzina_wyslania, Status)
                  VALUES (@IDZolnierza, @Tresc, @Typ, @DataIGodzina, @Status)",
                new MySqlParameter("@IDZolnierza", ZastepcaId),
                new MySqlParameter("@Tresc", $"Przydzielono Cię do służby w dniu {harmonogramItem.Data:yyyy-MM-dd}"),
                new MySqlParameter("@Typ", "Zastępstwo"),
                new MySqlParameter("@DataIGodzina", DateTime.Now),
                new MySqlParameter("@Status", "Wysłano"));

            // Powrót do listy harmonogramu, aby zobaczyć zmiany
            return RedirectToAction("HarmonogramKC", "Dyzurny");
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
