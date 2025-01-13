using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using WebApplication2.Controllers;
using WebApplication2.Models;

namespace WebApplication2.Tests
{
    [TestFixture]
    public class CalendarControllerTests
    {
        private ApplicationDbContext _dbContext;
        private CalendarController _controller;

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
                ImieOjca = "Adam"
            };

            var sluzba = new Sluzba
            {
                ID_Sluzby = 1,
                Rodzaj = "Warta",
                Przelozony = "Mjr. Kowalski",
                MiejscePelnieniaSluzby = "Baza B"
            };

            var harmonogram = new Harmonogram
            {
                ID_Harmonogram = 1,
                ID_Zolnierza = 1,
                Data = new DateTime(2025, 1, 4),
                Miesiac = "Styczeń",
                Sluzba = sluzba,
                Zolnierz = zolnierz
            };

            _dbContext.Zolnierze.Add(zolnierz);
            _dbContext.Sluzby.Add(sluzba);
            _dbContext.Harmonogramy.Add(harmonogram);
            _dbContext.SaveChanges();

            // Mockowanie użytkownika
            var claims = new List<Claim>
            {
                new Claim("ID_Zolnierza", "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller = new CalendarController(_dbContext)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };
        }

        [Test]
        public async Task Index_ShouldReturnNotFound_WhenUserIdIsNotSet()
        {
            // Arrange: Usuń dane użytkownika
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual("Nie znaleziono żołnierza.", notFoundResult.Value);
        }

        [Test]
        public async Task Index_ShouldReturnHarmonogramy_WhenUserHasEntries()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.Model);
            var model = viewResult.Model as List<Harmonogram>;
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("Jan", model.First().Zolnierz.Imie);
            Assert.AreEqual("Kowalski", model.First().Zolnierz.Nazwisko);
            Assert.AreEqual("Warta", model.First().Sluzba.Rodzaj);
            Assert.AreEqual("Baza B", model.First().Sluzba.MiejscePelnieniaSluzby);
        }

        [Test]
        public async Task Index_ShouldReturnEmptyList_WhenNoEntriesForUser()
        {
            // Usuń dane z bazy
            _dbContext.Harmonogramy.RemoveRange(_dbContext.Harmonogramy);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<Harmonogram>;
            Assert.IsEmpty(model);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _dbContext?.Dispose();
        }
    }
}
