using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using WebApplication2.Controllers;
using WebApplication2.Models;
using Moq;

namespace WebApplication2.Tests
{
    [TestFixture]
    public class ProfileControllerTests
    {
        private ApplicationDbContext _dbContext;
        private ProfileController _controller;

        [SetUp]
        public void SetUp()
        {
            // Generowanie unikalnej nazwy bazy danych dla każdego testu
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // Dodanie przykładowych danych
            var pododdzial = new Pododdzial
            {
                ID_Pododdzialu = 1,
                Nazwa = "TestowyPododdzial",
                KomorkaNadrzedna = "KomórkaX",
                Dowodca = "Jan.Kowalski",
                NrKontaktowy = "123456789"
            };
            _dbContext.Pododdzialy.Add(pododdzial);

            var zolnierz = new Zolnierz
            {
                ID_Zolnierza = 10,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Stopien = "Kapitan",
                Wiek = 30,
                Adres = "Ul. Kwiatowa 12, Warszawa",
                ImieOjca = "Adam",
                ID_Pododdzialu = 1,
                Pesel = "12345678901",
                Punkty = 50,
                Pododdzial = pododdzial
            };
            _dbContext.Zolnierze.Add(zolnierz);

            var login = new Login
            {
                ID_Loginu = 100,
                ID_Zolnierza = 10,
                LoginName = "Jan.Kowalski",
                Haslo = "hashed_password",
                Email = "jan.kowalski@example.com",
                Zolnierz = zolnierz
            };
            _dbContext.Login_dane.Add(login);

            _dbContext.SaveChanges();

            // Tworzenie kontrolera
            _controller = new ProfileController(_dbContext);

            // Mockowanie HttpContext z claimem ID_Zolnierza
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("ID_Zolnierza", "10")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
            _controller.Dispose();
        }

        #region Index Tests


        [Test]
        public async Task Index_ReturnsNotFound_WhenZolnierzDoesNotExist()
        {
            // Arrange
            // Usunięcie żołnierza z bazy danych
            var zolnierz = await _dbContext.Zolnierze.FindAsync(10);
            _dbContext.Zolnierze.Remove(zolnierz);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result, "Expected a NotFoundObjectResult.");
            Assert.AreEqual("Nie znaleziono żołnierza.", result.Value);
        }

        [Test]
        public async Task Index_RedirectsToLogout_WhenID_ZolnierzaClaimIsInvalid()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("ID_Zolnierza", "invalid") // Nieprawidłowa wartość
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Act
            var result = await _controller.Index() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result, "Expected a RedirectToActionResult.");
            Assert.AreEqual("Logout", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        [Test]
        public async Task Index_RedirectsToLogout_WhenID_ZolnierzaClaimIsMissing()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                // Brak claimu ID_Zolnierza
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Act
            var result = await _controller.Index() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result, "Expected a RedirectToActionResult.");
            Assert.AreEqual("Logout", result.ActionName);
            Assert.AreEqual("Account", result.ControllerName);
        }

        #endregion

        #region Edit GET Tests

        [Test]
        public async Task Edit_GET_ReturnsView_WithZolnierzModel_WhenZolnierzExists()
        {
            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result, "Expected a ViewResult.");
            var model = result.Model as Zolnierz;
            Assert.IsNotNull(model, "Expected a Zolnierz model.");
            Assert.AreEqual(10, model.ID_Zolnierza);
            Assert.AreEqual("Jan", model.Imie);
            Assert.AreEqual("Kowalski", model.Nazwisko);
            Assert.AreEqual("Kapitan", model.Stopien);
            Assert.AreEqual(30, model.Wiek);
            Assert.AreEqual("Ul. Kwiatowa 12, Warszawa", model.Adres);
            Assert.AreEqual("Adam", model.ImieOjca);
            Assert.AreEqual("12345678901", model.Pesel);
            Assert.AreEqual(50, model.Punkty);
        }

        [Test]
        public async Task Edit_GET_ReturnsNotFound_WhenZolnierzDoesNotExist()
        {
            // Arrange
            // Usunięcie żołnierza z bazy danych
            var zolnierz = await _dbContext.Zolnierze.FindAsync(10);
            _dbContext.Zolnierze.Remove(zolnierz);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result, "Expected a NotFoundObjectResult.");
            Assert.AreEqual("Nie znaleziono żołnierza.", result.Value);
        }

        

        #endregion

        #region Edit POST Tests

       

        [Test]
        public async Task Edit_POST_InvalidData_ReturnsViewWithErrors()
        {
            // Arrange
            string invalidStopien = ""; // Wymagane
            int invalidWiek = 17; // Poniżej minimalnego wieku
            string invalidAdres = ""; // Wymagane

            // Act
            var result = await _controller.Index(invalidStopien, invalidWiek, invalidAdres) as ViewResult;

            // Assert
            Assert.IsNotNull(result, "Expected a ViewResult.");
            Assert.IsFalse(_controller.ModelState.IsValid, "ModelState should be invalid.");
            Assert.AreEqual(3, _controller.ModelState.ErrorCount, "Expected three validation errors.");

            var model = result.Model as Zolnierz;
            Assert.IsNotNull(model, "Expected a Zolnierz model.");
            Assert.AreEqual(invalidStopien, model.Stopien);
            Assert.AreEqual(invalidWiek, model.Wiek);
            Assert.AreEqual(invalidAdres, model.Adres);
        }

       

        [Test]
        public async Task Edit_POST_ReturnsNotFound_WhenID_ZolnierzaClaimIsMissing()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                // Brak claimu ID_Zolnierza
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            string newStopien = "Major";
            int newWiek = 35;
            string newAdres = "Ul. Nowa 10, Warszawa";

            // Act
            var result = await _controller.Index(newStopien, newWiek, newAdres) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(result, "Expected a NotFoundObjectResult.");
            Assert.AreEqual("Nie znaleziono żołnierza.", result.Value);
        }


        #endregion
    }
}
