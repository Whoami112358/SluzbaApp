using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO; // do testowania Download()
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication2.Controllers;
using WebApplication2.Models;

namespace WebApplication2.Tests
{
    [TestFixture]
    public class DowodcaControllerTests : IDisposable
    {
        private ApplicationDbContext _dbContext;
        private DowodcaController _controller;
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IHostEnvironment> _hostEnvironmentMock;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            SeedDatabase();

            _memoryCacheMock = new Mock<IMemoryCache>();
            _hostEnvironmentMock = new Mock<IHostEnvironment>();
            _hostEnvironmentMock.Setup(env => env.ContentRootPath).Returns("C:\\FakePath");

            _controller = new DowodcaController(_dbContext, _hostEnvironmentMock.Object, _memoryCacheMock.Object);

            // Ustawiamy zalogowanego dowódcę (login = "Jan.Kowalski")
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Jan.Kowalski") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Jeśli kontroler używa TempData:
            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
        }

        public void Dispose()
        {
            TearDown();
        }

        private void SeedDatabase()
        {
            // Przygotowujemy dane testowe: Pododdział, Żołnierz (dowódca), inny Żołnierz, Służba, Harmonogram itd.
            var pododdzial = new Pododdzial
            {
                ID_Pododdzialu = 1,
                Nazwa = "TestowyPododdzial",
                KomorkaNadrzedna = "KomórkaX",
                Dowodca = "Jan.Kowalski",
                NrKontaktowy = "123456789"
            };
            _dbContext.Pododdzialy.Add(pododdzial);

            var dowodca = new Zolnierz
            {
                ID_Zolnierza = 10,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Stopien = "Kapitan",
                ID_Pododdzialu = pododdzial.ID_Pododdzialu,
                Punkty = 50,
                Pesel = "12345678901",
                ImieOjca = "Adam",
                Adres = "Ul. Kwiatowa 12, Warszawa"
            };
            _dbContext.Zolnierze.Add(dowodca);

            var zolnierz = new Zolnierz
            {
                ID_Zolnierza = 11,
                Imie = "Adam",
                Nazwisko = "Nowak",
                Stopien = "Sierżant",
                ID_Pododdzialu = pododdzial.ID_Pododdzialu,
                Punkty = 30,
                Pesel = "09876543210",
                ImieOjca = "Piotr",
                Adres = "Ul. Słoneczna 5, Warszawa"
            };
            _dbContext.Zolnierze.Add(zolnierz);

            var sluzba = new Sluzba
            {
                ID_Sluzby = 100,
                Rodzaj = "Operacyjna",
                Przelozony = "Komendant X",
                MiejscePelnieniaSluzby = "Baza A"
            };
            _dbContext.Sluzby.Add(sluzba);

            var priorytet = new Priorytet
            {
                ID_Priorytetu = 4000,
                ID_Zolnierza = dowodca.ID_Zolnierza,
                ID_Sluzby = sluzba.ID_Sluzby,
                PriorytetValue = 1
            };
            _dbContext.Priorytety.Add(priorytet);

            var harmonogram = new Harmonogram
            {
                ID_Harmonogram = 1000,
                ID_Zolnierza = zolnierz.ID_Zolnierza,
                ID_Sluzby = sluzba.ID_Sluzby,
                Data = new DateTime(2025, 1, 10),
                Miesiac = "Styczeń",
                Zolnierz = zolnierz,
                Sluzba = sluzba
            };
            _dbContext.Harmonogramy.Add(harmonogram);

            // Dodajmy też np. jedną służbę w harmonogramie dowódcy (żeby wykazać, że ma dane)
            var harmonogramDowodca = new Harmonogram
            {
                ID_Harmonogram = 2000,
                ID_Zolnierza = dowodca.ID_Zolnierza,
                ID_Sluzby = sluzba.ID_Sluzby,
                Data = new DateTime(2025, 1, 12),
                Miesiac = "Styczeń",
                Zolnierz = dowodca,
                Sluzba = sluzba
            };
            _dbContext.Harmonogramy.Add(harmonogramDowodca);

            _dbContext.SaveChanges();
        }

        #region HarmonogramKC Tests

        [Test]
        public void HarmonogramKC_ShouldReturnViewWithModel_WhenDowodcaFound()
        {
            // Act
            var result = _controller.HarmonogramKC();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;
            Assert.IsNotNull(viewResult.Model);

            // Sprawdzamy, czy model to lista Harmonogram
            var model = viewResult.Model as List<Harmonogram>;
            Assert.IsNotNull(model);
            Assert.IsTrue(model.Count >= 1, "Powinien być co najmniej 1 wpis w harmonogramie (seed).");
        }

        [Test]
        public void HarmonogramKC_ShouldReturnNotFound_WhenDowodcaNotInDatabase()
        {
            // Arrange
            // Sfałszujemy tożsamość na "Ktos.Inny" - nie ma takiego w seed
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Ktos.Inny") };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _controller.HarmonogramKC();

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFoundResult = (NotFoundObjectResult)result;
            Assert.AreEqual("Nie znaleziono żołnierza o tym imieniu i nazwisku.", notFoundResult.Value);
        }

        [Test]
        public void HarmonogramKC_ShouldReturnBadRequest_WhenLoginFormatIsIncorrect()
        {
            // Arrange
            // Zamiast "Imie.Nazwisko" damy np. "BlednyFormat"
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "BlednyFormat") };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _controller.HarmonogramKC();

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.AreEqual("Niepoprawny format loginu.", badRequestResult.Value);
        }

        #endregion

        #region DodajHarmonogramKC Tests

        [Test]
        public async Task DodajHarmonogramKC_Get_ShouldReturnViewWithZolnierzeISluzby()
        {
            // Act
            var result = await _controller.DodajHarmonogramKC();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            // Sprawdzamy, czy ViewBag ma listy Zolnierze i Sluzby
            Assert.IsTrue(viewResult.ViewData.ContainsKey("Zolnierze"));
            Assert.IsTrue(viewResult.ViewData.ContainsKey("Sluzby"));
        }


        [Test]
        public async Task DodajHarmonogramKC_Post_ShouldNotInsert_WhenSoldierNotFound()
        {
            // Arrange
            var newHarm = new Harmonogram
            {
                ID_Zolnierza = 9999, // nieistniejący żołnierz
                ID_Sluzby = 100,
                Data = DateTime.Now,
                Miesiac = "Luty"
            };

            // Act
            var result = await _controller.DodajHarmonogramKC(newHarm);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("HarmonogramKC", redirectResult.ActionName);

            // Sprawdzamy, czy NIE dodało do bazy
            var inserted = _dbContext.Harmonogramy.FirstOrDefault(h => h.ID_Zolnierza == 9999);
            Assert.IsNull(inserted, "Rekord o nieistniejącym żołnierzu nie powinien się wstawić.");
        }

        #endregion

        #region Punktacja Tests

        [Test]
        public void Punktacja_ShouldReturnViewWithZolnierze_WhenDowodcaFound()
        {
            // Act
            var result = _controller.Punktacja("Imie");

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as List<Zolnierz>;

            Assert.IsNotNull(model);
            Assert.IsTrue(model.Count > 0, "Powinien być co najmniej jeden żołnierz w pododdziale dowódcy.");
            Assert.IsTrue(model.Any(z => z.Imie == "Adam"), "W seedzie Adam Nowak jest w tym samym pododdziale co Jan.Kowalski.");
        }
        /*
        [Test]
        public void Punktacja_ShouldReturnNotFound_WhenDowodcaNotInDatabase()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "NieMa.TeGo") };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _controller.Punktacja("Imie");

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFound = (NotFoundObjectResult)result;
            Assert.AreEqual("Nie znaleziono żołnierza o tym imieniu i nazwisku.", notFound.Value);
        }

        [Test]
        public void Punktacja_ShouldReturnBadRequest_WhenLoginFormatInvalid()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "ZlyLogin") };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _controller.Punktacja("Imie");

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badReq = (BadRequestObjectResult)result;
            Assert.AreEqual("Niepoprawny format loginu.", badReq.Value);
        }
        */
        #endregion

        #region DodajPunkty Tests

        [Test]
        public void DodajPunkty_ShouldIncreasePoints_WhenSoldierFound()
        {
            // Arrange
            var soldierBefore = _dbContext.Zolnierze.Find(11); // Adam Nowak, 30 pkt
            int initialPoints = soldierBefore.Punkty;

            // Act
            var result = _controller.DodajPunkty(11, 10) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Punktacja", result.ActionName);

            // Sprawdzamy, czy punkty zostały zwiększone
            var soldierAfter = _dbContext.Zolnierze.Find(11);
            Assert.AreEqual(initialPoints + 10, soldierAfter.Punkty);
        }

        [Test]
        public void DodajPunkty_ShouldDoNothing_WhenSoldierNotFound()
        {
            // Act
            var result = _controller.DodajPunkty(999, 10) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Punktacja", result.ActionName);

            // W bazie nie ma żołnierza 999, więc nic nie zmieniono
            // Wystarczy sprawdzić, czy nie ma błędu/wyjątku i nadal jest redirect
        }

        #endregion
        /*
        #region ZaktualizujPunkty Tests

        
        [Test]
        public void ZaktualizujPunkty_ShouldAddPoints_WhenSoldierFound()
        {
            // Arrange
            var soldier = _dbContext.Zolnierze.Find(11); // 30 pkt
            int initialPoints = soldier.Punkty;

            // Act
            var result = _controller.ZaktualizujPunkty(11, 5) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Punktacja", result.ActionName);

            var after = _dbContext.Zolnierze.Find(11);
            Assert.AreEqual(initialPoints + 5, after.Punkty);
        }

        [Test]
        public void ZaktualizujPunkty_ShouldNotRemoveMorePointsThanSoldierHas()
        {
            // Arrange
            var soldier = _dbContext.Zolnierze.Find(11); // 30 pkt
            int initialPoints = soldier.Punkty;

            // Act
            var result = _controller.ZaktualizujPunkty(11, -40) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Punktacja", result.ActionName);

            // Sprawdzamy, czy punkty nie zmniejszyły się (bo soldier ma 30, a chciał -40)
            var after = _dbContext.Zolnierze.Find(11);
            Assert.AreEqual(initialPoints, after.Punkty);

            // Dodatkowo w TempData["Error"] powinna być stosowna informacja
            Assert.IsTrue(_controller.TempData.ContainsKey("Error"));
            Assert.AreEqual("Nie można usunąć więcej punktów niż aktualnie posiadane.", _controller.TempData["Error"]);
        }

        [Test]
        public void ZaktualizujPunkty_ShouldShowError_WhenSoldierNotFound()
        {
            // Act
            var result = _controller.ZaktualizujPunkty(9999, 10) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Punktacja", result.ActionName);
            Assert.AreEqual("Żołnierz o podanym identyfikatorze nie został znaleziony.",
                _controller.TempData["Error"]);
        }
        #endregion
        */

        #region ListaZwolnien Tests

        [Test]
        public async Task ListaZwolnien_ShouldReturnViewWithZwolnienia_WhenDowodcaFound()
        {
            // Act
            var result = await _controller.ListaZwolnien();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            // Model to lista Zwolnien
            var model = viewResult.Model as List<Zwolnienie>;
            Assert.IsNotNull(model, "Lista zwolnień nie powinna być null, nawet jeśli pusta.");

            // Powinny być też w ViewBag dostępni żołnierze
            Assert.IsTrue(viewResult.ViewData.ContainsKey("Zolnierze"));
        }

        [Test]
        public async Task ListaZwolnien_ShouldReturnNotFound_WhenDowodcaNotExist()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Ktos.Nieistniejacy") };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = await _controller.ListaZwolnien();

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFound = (NotFoundObjectResult)result;
            Assert.AreEqual("Nie znaleziono żołnierza o tym imieniu i nazwisku.", notFound.Value);
        }

        #endregion

        #region DodajZwolnienie Tests


        [Test]
        public async Task DodajZwolnienie_Post_ShouldReturnView_WhenSoldierNotFound()
        {
            // Arrange
            var zwolnienie = new Zwolnienie
            {
                ID_Zolnierza = 9999,
                DataRozpoczeciaZwolnienia = DateTime.Now,
                DataZakonczeniaZwolnienia = DateTime.Now.AddDays(1)
            };

            // Act
            var result = await _controller.DodajZwolnienie(zwolnienie);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            // Powinno przejść do widoku "ListaZwolnien" z parametrem modelu
            Assert.AreEqual("ListaZwolnien", viewResult.ViewName);
            var model = viewResult.Model as Zwolnienie;
            Assert.IsNotNull(model);
            Assert.AreEqual(9999, model.ID_Zolnierza);

            // W bazie nie powinno być takiego zwolnienia
            var inserted = _dbContext.Zwolnienia.FirstOrDefault(z => z.ID_Zolnierza == 9999);
            Assert.IsNull(inserted);
        }

        #endregion

        #region ListaPowiadomien Tests

        [Test]
        public async Task ListaPowiadomien_ShouldReturnViewWithPowiadomienia_WhenDowodcaFound()
        {
            // Act
            var result = await _controller.ListaPowiadomien(null, null);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            // Model to lista Powiadomienie
            var model = viewResult.Model as List<Powiadomienie>;
            Assert.IsNotNull(model);
            // Może być pusta, bo nie seedowaliśmy Powiadomienia,
            // ale ważne, że to jest poprawny typ i widok.
        }

        #endregion


        #region ZarzadzajPriorytetami (GET) Tests

        [Test]
        public async Task ZarzadzajPriorytetami_Get_ShouldReturnViewWithPriorytety_WhenDowodcaFound()
        {
            // Act
            var result = await _controller.ZarzadzajPriorytetami();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            var model = viewResult.Model as List<Priorytet>;
            Assert.IsNotNull(model);
            Assert.IsTrue(model.Any(), "Powinniśmy mieć co najmniej 1 priorytet z bazy.");
        }

        [Test]
        public async Task ZarzadzajPriorytetami_Get_ShouldReturnNotFound_WhenDowodcaNotExist()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Ktos.Nieistniejacy") };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = await _controller.ZarzadzajPriorytetami();

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFound = (NotFoundObjectResult)result;
            Assert.AreEqual("Nie znaleziono dowódcy o podanym loginie.", notFound.Value);
        }

        #endregion

        #region ZarzadzajPriorytetami (POST) Tests 
        // (już były w kodzie, pozostawiamy je bez zmian)

        [Test]
        public async Task ZarzadzajPriorytetami_Post_ShouldUpdatePriorytety_WhenValid()
        {
            var priorytety = new List<Priorytet>
            {
                new Priorytet
                {
                    ID_Priorytetu = 4000,
                    ID_Zolnierza = 10,
                    ID_Sluzby = 100,
                    PriorytetValue = 3
                }
            };

            var result = await _controller.ZarzadzajPriorytetami(priorytety) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("HarmonogramKC", result.ActionName);

            var updatedPriorytet = await _dbContext.Priorytety
                .FirstOrDefaultAsync(p => p.ID_Priorytetu == 4000);

            Assert.IsNotNull(updatedPriorytet);
            Assert.AreEqual(3, updatedPriorytet.PriorytetValue);
        }

        [Test]
        public async Task ZarzadzajPriorytetami_Post_ShouldReturnError_WhenInvalidModel()
        {
            var priorytety = new List<Priorytet>
            {
                new Priorytet
                {
                    ID_Priorytetu = 4000,
                    ID_Zolnierza = 10,
                    ID_Sluzby = 100,
                    PriorytetValue = 6
                }
            };

            _controller.ModelState.AddModelError("PriorytetValue", "Priorytet musi być w zakresie od 1 do 5.");

            var result = await _controller.ZarzadzajPriorytetami(priorytety) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["PriorytetValue"].Errors.Any(e => e.ErrorMessage.Contains("Priorytet musi być w zakresie od 1 do 5.")));
        }

        #endregion

        /*
        #region AnalizaDostepnosci (GET) Tests
        
        [Test]
        public void AnalizaDostepnosci_Get_ShouldReturnView()
        {
            // Act
            var result = _controller.AnalizaDostepnosci(10, 2001);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            // Sprawdzamy, czy w ViewBag są listy miesięcy/lat
            Assert.IsTrue(viewResult.ViewData.ContainsKey("Months"));
            Assert.IsTrue(viewResult.ViewData.ContainsKey("Years"));
        }

        #endregion
        */
        #region AnalizaDostepnosci (POST) Tests 
        // (już były w kodzie – "AnalizaDostepnosci_Post_ShouldGenerateEvents_WhenValid")

        [Test]
        public async Task AnalizaDostepnosci_Post_ShouldGenerateEvents_WhenValid()
        {
            var result = await _controller.AnalizaDostepnosci(1, 2025) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsTrue(result.ViewData.ContainsKey("EventsJson"));
        }

        #endregion
    }
}
