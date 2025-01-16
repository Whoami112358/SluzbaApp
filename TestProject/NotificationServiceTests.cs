using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using WebApplication2.Models;
using WebApplication2.Services;

namespace WebApplication2.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private IServiceProvider _serviceProvider;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void SetUp()
        {
            var jutro = DateTime.UtcNow.AddDays(1).Date; // Użyj daty jutro na podstawie UTC

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            var zolnierz = new Zolnierz
            {
                ID_Zolnierza = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                ImieOjca = "Adam",
                Pesel = "12345678901",
                Stopien = "Sier"
            };

            var sluzba = new Sluzba
            {
                ID_Sluzby = 1,
                Rodzaj = "Warta",
                MiejscePelnieniaSluzby = "Baza A",
                Przelozony = "Mjr. Nowak"
            };

            var harmonogram = new Harmonogram
            {
                ID_Harmonogram = 1,
                ID_Zolnierza = 1,
                ID_Sluzby = 1,
                Data = jutro, // Dopasowana data
                Miesiac = jutro.ToString("MMMM"),
                Zolnierz = zolnierz,
                Sluzba = sluzba
            };

            _dbContext.Zolnierze.Add(zolnierz);
            _dbContext.Sluzby.Add(sluzba);
            _dbContext.Harmonogramy.Add(harmonogram);
            _dbContext.SaveChanges();

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("TestDb"));
            services.AddLogging();
            _serviceProvider = services.BuildServiceProvider();
        }


        [Test]
        public async Task SendNotificationsAsync_ShouldHandleEmptyHarmonogramWithoutCrashing()
        {
            // Arrange
            _dbContext.Harmonogramy.RemoveRange(_dbContext.Harmonogramy);
            await _dbContext.SaveChangesAsync();

            var notificationService = new NotificationService(_serviceProvider);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await notificationService.SendNotificationsAsync(),
                "Metoda nie powinna rzucać wyjątku nawet przy pustym harmonogramie.");
        }


        [Test]
        public async Task SendNotificationsAsync_ShouldNotGenerateNotifications_WhenNoServiceScheduled()
        {
            _dbContext.Harmonogramy.RemoveRange(_dbContext.Harmonogramy);
            await _dbContext.SaveChangesAsync();

            var notificationService = new NotificationService(_serviceProvider);
            await notificationService.SendNotificationsAsync();

            var notifications = _dbContext.Powiadomienia.ToList();
            Assert.AreEqual(0, notifications.Count);
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _dbContext?.Dispose();
        }
    }
}
