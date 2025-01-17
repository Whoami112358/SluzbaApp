using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication2.Controllers; // <-- zmień na właściwą przestrzeń nazw
using WebApplication2.Models;      // <-- zmień na właściwą przestrzeń nazw

namespace WebApplication2.Tests
{
    [TestFixture]
    public class ScheduleControllerTests
    {
        private ApplicationDbContext _dbContext;
        private ScheduleController _controller;

        [SetUp]
        public void SetUp()
        {
            // 1. Konfiguracja InMemoryDb
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // 2. Dodaj przykładowe dane
            SeedDatabase();

            // 3. Inicjalizacja kontrolera
            _controller = new ScheduleController(_dbContext)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Jeśli w kontrolerze potrzebna jest TempData, można je mockować:
            // _controller.TempData = new TempDataDictionary(
            //     _controller.ControllerContext.HttpContext,
            //     Mock.Of<ITempDataProvider>()
            // );
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
            _controller?.Dispose();
        }

        /// <summary>
        /// Metoda pomocnicza do seedowania danych w InMemoryDb.
        /// </summary>
        private void SeedDatabase()
        {
            var sluzba = new Sluzba
            {
                ID_Sluzby = 1,
                Rodzaj = "Warta",
                Przelozony = "Mjr. Kowalski",
                MiejscePelnieniaSluzby = "Baza B"
            };

            var zolnierz = new Zolnierz
            {
                ID_Zolnierza = 10,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Stopien = "Sierżant",
                Adres = "Ul. Kwiatowa 12, Warszawa",
                Pesel = "12345678901",
                ImieOjca = "Adam"
            };

            var harmonogram = new Harmonogram
            {
                ID_Harmonogram = 100, // ID przykładowego harmonogramu
                ID_Zolnierza = zolnierz.ID_Zolnierza,
                Data = new DateTime(2025, 1, 10),
                Miesiac = "Styczeń",
                Sluzba = sluzba,
                Zolnierz = zolnierz
            };

            _dbContext.Sluzby.Add(sluzba);
            _dbContext.Zolnierze.Add(zolnierz);
            _dbContext.Harmonogramy.Add(harmonogram);
            _dbContext.SaveChanges();
        }

        // ------------------------------------------------------------
        //  TESTY ADDREMINDER (GET)
        // ------------------------------------------------------------

        [Test]
        public async Task AddReminder_Get_ShouldReturnView_WhenValidId()
        {
            // Arrange
            int validId = 100; // mamy taki w seedzie

            // Act
            var result = await _controller.AddReminder(validId);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;

            // Sprawdzamy, czy model to AddReminderViewModel
            Assert.IsNotNull(viewResult.Model);
            Assert.IsInstanceOf<AddReminderViewModel>(viewResult.Model);

            // Możemy też sprawdzić wartość w polach ViewBag (lub modelu)
            var model = (AddReminderViewModel)viewResult.Model;
            Assert.AreEqual(validId, model.ID_Harmonogram);
            Assert.IsTrue(model.Tytul.Contains("Służba"));
            Assert.AreEqual(new DateTime(2025, 1, 10), model.DataSluzby);
            Assert.AreEqual(TimeSpan.Zero, model.GodzinaSluzby);
            Assert.AreEqual(60, model.OffsetMinutes);
        }

        [Test]
        public async Task AddReminder_Get_ShouldReturnNotFound_WhenInvalidId()
        {
            // Arrange
            int invalidId = 9999; // taki raczej nie istnieje w seedzie

            // Act
            var result = await _controller.AddReminder(invalidId);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual("Nie znaleziono służby w harmonogramie.", notFoundResult.Value);
        }

        // ------------------------------------------------------------
        //  TESTY ADDREMINDER (POST)
        // ------------------------------------------------------------

        [Test]
        public void AddReminder_Post_ShouldReturnView_AddReminderResult_WhenModelIsValid()
        {
            // Arrange
            var model = new AddReminderViewModel
            {
                ID_Harmonogram = 100,   // istniejący
                DataSluzby = new DateTime(2025, 1, 10),
                GodzinaSluzby = new TimeSpan(9, 0, 0),
                Tytul = "Moja służba",
                Notatki = "Zaopatrzyć się w prowiant",
                OffsetMinutes = 30
            };

            // Act
            var result = _controller.AddReminder(model);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;

            // Sprawdzamy, czy zwrócono widok "AddReminderResult"
            Assert.AreEqual("AddReminderResult", viewResult.ViewName);

            // Sprawdzamy, czy w ViewBag jest link
            Assert.IsTrue(viewResult.ViewData.ContainsKey("GoogleLink"));

            var googleLink = viewResult.ViewData["GoogleLink"] as string;
            Assert.IsNotNull(googleLink);
            Assert.IsTrue(googleLink.StartsWith("https://calendar.google.com/calendar/render"));
            Assert.IsTrue(googleLink.Contains("Moja%20s%C5%82u%C5%BCba"));
        }

        [Test]
        public void AddReminder_Post_ShouldReturnSameView_WhenModelIsInvalid()
        {
            // Arrange
            var model = new AddReminderViewModel
            {
                ID_Harmonogram = 100,
                // Brakuje np. Tytul lub innego wymaganego pola, można też dodać błąd walidacji "ręcznie"
            };

            // Dodajemy sztuczny błąd walidacji
            _controller.ModelState.AddModelError("Tytul", "Tytuł jest wymagany.");

            // Act
            var result = _controller.AddReminder(model);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;

            // Ponieważ ModelState jest nieprawidłowy, powinniśmy pozostać w tym samym widoku (domyślnie "AddReminder")
            // W praktyce jeśli w kontrolerze nie ma nazwy widoku w `return View(model)`,
            // to jest to nazwa akcji = "AddReminder". 
            // Ale tu można sprawdzić w inny sposób, np.:
            Assert.IsNull(viewResult.ViewName);
            // lub: Assert.AreEqual("", viewResult.ViewName);

            // Sprawdzamy, czy błąd walidacji jest obecny
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState.ContainsKey("Tytul"));
        }

        [Test]
        public void AddReminder_Post_ShouldReturnError_WhenHarmonogramNotFound()
        {
            // Arrange
            var model = new AddReminderViewModel
            {
                ID_Harmonogram = 9999, // nieistniejący ID
                DataSluzby = DateTime.Now,
                GodzinaSluzby = new TimeSpan(10, 0, 0),
                Tytul = "Służba testowa",
                OffsetMinutes = 30
            };

            // Act
            var result = _controller.AddReminder(model);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = (ViewResult)result;

            // Powinniśmy zobaczyć błąd w ModelState (ModelState.AddModelError("", "..."))
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState.ContainsKey(string.Empty),
                "Oczekiwano błędu na kluczu '', oznaczającego błąd ogólny.");
        }
    }
}
