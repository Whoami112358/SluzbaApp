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
        public IActionResult DyzurnyView()
        {
            return View();
        }
        public IActionResult ListaTygodniowa()
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
        public IActionResult Punktacja()
        {
            var zolnierze = _context.Zolnierze.ToList();
            return View(zolnierze);
        }

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
        public IActionResult HarmonogramKC()
        {
            var harmonogram = _context.Harmonogramy
                                        .Include(h => h.Zolnierz)  // Ładowanie powiązanych danych żołnierza
                                        .Include(h => h.Sluzba)    // Ładowanie powiązanych danych służby
                                        .OrderBy(h => h.Data)
                                        .ToList();

            _memoryCache.Set("Harmonogram", harmonogram, TimeSpan.FromMinutes(10));
            return View(harmonogram);  // Zwracanie widoku z danymi harmonogramu
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

    }
}
