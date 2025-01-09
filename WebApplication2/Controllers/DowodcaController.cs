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
    }
}
