using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WebApplication2.Controllers;
using WebApplication2.Models;

namespace WebApplication2.Tests
{
    [TestFixture]
    public class PowiadomieniaControllerTests
    {
        private ApplicationDbContext _dbContext;
        private PowiadomieniaController _controller;

        [SetUp]
        public void SetUp()
        {
            // Konfiguracja bazy danych InMemory
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // Dodanie przykładowych danych do bazy
            var zolnierz = new Zolnierz
            {
                ID_Zolnierza = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Stopien = "Sierżant",
                Adres = "Ul. Kwiatowa 12, Warszawa",
                Pesel = "12345678901",
                ImieOjca = "Adam",
                Punkty = 10,
                Wiek = 30
            };

            var powiadomienie = new Powiadomienie
            {
                ID_Powiadomienia = 1,
                ID_Zolnierza = 1,
                TrescPowiadomienia = "Testowe powiadomienie",
                TypPowiadomienia = "Przypomnienie", // Wymagane pole
                DataIGodzinaWyslania = DateTime.Now,
                Status = "Nowe"
            };


            _dbContext.Zolnierze.Add(zolnierz);
            _dbContext.Powiadomienia.Add(powiadomienie);
            _dbContext.SaveChanges();

            // Mockowanie użytkownika
            var claims = new List<Claim>
    {
        new Claim("ID_Zolnierza", "1")
    };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller = new PowiadomieniaController(_dbContext)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };
        }



        [Test]
        public async Task Index_ShouldReturnPowiadomienia()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<Powiadomienie>;

            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("Testowe powiadomienie", model.First().TrescPowiadomienia);
            Assert.AreEqual("Przypomnienie", model.First().TypPowiadomienia); // Oczekiwany tekst
        }



        [Test]
        public async Task Odbierz_ShouldSetStatusToOdebrane_WhenPowiadomienieExists()
        {
            // Act
            var result = await _controller.Odbierz(1);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var updatedPowiadomienie = await _dbContext.Powiadomienia.FindAsync(1);
            Assert.AreEqual("Odebrane", updatedPowiadomienie.Status);
        }

        [Test]
        public async Task Odbierz_ShouldReturnNotFound_WhenPowiadomienieDoesNotExist()
        {
            // Act
            var result = await _controller.Odbierz(999);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual("Nie znaleziono powiadomienia o podanym ID.", notFoundResult.Value);
        }

        [Test]
        public async Task Odbierz_ShouldReturnForbid_WhenPowiadomienieBelongsToAnotherZolnierz()
        {
            // Arrange
            var otherPowiadomienie = new Powiadomienie
            {
                ID_Powiadomienia = 2,
                ID_Zolnierza = 2,
                TrescPowiadomienia = "Inne przypomnienie",
                DataIGodzinaWyslania = DateTime.Now,
                Status = "Niewysłano"
            };
            _dbContext.Powiadomienia.Add(otherPowiadomienie);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Odbierz(2);

            // Assert
            Assert.IsInstanceOf<ForbidResult>(result);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _dbContext?.Dispose();
        }
    }
}
