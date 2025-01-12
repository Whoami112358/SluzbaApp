using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using WebApplication2.Models;
using MySqlConnector;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font;
using iText.Kernel.Font;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NUnit.Framework;
using WebApplication2.Controllers;
using System.Net.Sockets;

[TestFixture]
public class DyzurnyControllerTests
{
    private Mock<IMemoryCache> _memoryCacheMock;
    private ApplicationDbContext _dbContext;
    private Mock<IHostEnvironment> _hostEnvironmentMock;
    private DyzurnyController _controller;

    [SetUp]
    public void SetUp()
    {
        
    }

    [Test]
    public void Download_ShouldReturnPdfFile_WhenDataIsAvailableInCache()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
             .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
             .Options;

        _dbContext = new ApplicationDbContext(options);

        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(env => env.ContentRootPath).Returns(AppDomain.CurrentDomain.BaseDirectory);

        // Arrange
        var mockData = new List<Harmonogram>
        {
            new Harmonogram
        {
            ID_Harmonogram = 1,
            ID_Zolnierza = 2,
            Data = DateTime.Now,
            Miesiac = DateTime.Now.Month.ToString(),
            Zolnierz = new Zolnierz { Imie = "John", Nazwisko = "Doe", Stopien="kapral", Pesel="01234567891011", ImieOjca="Marek" },
            Sluzba = new Sluzba {ID_Sluzby=1, Przelozony="oficer dyzurny", MiejscePelnieniaSluzby="Wartownia", Rodzaj = "Guard Duty" }}
        };

        // Setup memory cache to return mock data
        _memoryCacheMock = new Mock<IMemoryCache>();
        object outData = null;
        _memoryCacheMock.Setup(m => m.TryGetValue("Harmonogram", out outData))
            .Callback((object key, out object value) =>
            {
                value = mockData;
            })
            .Returns(true);

        _controller = new DyzurnyController(_dbContext, _hostEnvironmentMock.Object, _memoryCacheMock.Object);
        // Act
        var result = _controller.Download() as FileContentResult;

        // Assert
        Assert.IsNotNull(result, "Expected a FileContentResult.");
        Assert.AreEqual("application/pdf", result.ContentType);
        Assert.AreEqual("Harmonogram.pdf", result.FileDownloadName);
        Assert.IsTrue(result.FileContents.Length > 0, "Generated PDF should not be empty.");
    }

    [Test]
    public void Download_ShouldFallbackToDatabase_WhenCacheIsEmpty()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
             .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
             .Options;

        _dbContext = new ApplicationDbContext(options);

        // Seed data if necessary
        _dbContext.Harmonogramy.Add(new Harmonogram
        {
            ID_Harmonogram = 1,
            ID_Zolnierza = 2,
            Data = DateTime.Now,
            Miesiac = DateTime.Now.Month.ToString(),
            Zolnierz = new Zolnierz { Imie = "John", Nazwisko = "Doe", Stopien = "kapral", Pesel = "01234567891011", ImieOjca = "Marek" },
            Sluzba = new Sluzba { ID_Sluzby = 1, Przelozony = "oficer dyzurny", MiejscePelnieniaSluzby = "Wartownia", Rodzaj = "Guard Duty" }
        });
        _dbContext.SaveChanges();

        // Mock other dependencies
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(env => env.ContentRootPath).Returns(AppDomain.CurrentDomain.BaseDirectory);

        _memoryCacheMock = new Mock<IMemoryCache>();
        object cachedData = null;
        _memoryCacheMock.Setup(m => m.TryGetValue("Harmonogram", out cachedData))
            .Returns(false);

        _controller = new DyzurnyController(_dbContext, _hostEnvironmentMock.Object, _memoryCacheMock.Object);

        // Act
        var result = _controller.Download() as FileContentResult;

        // Assert
        Assert.IsNotNull(result, "Expected a FileContentResult.");
        Assert.AreEqual("application/pdf", result.ContentType);
        Assert.IsTrue(result.FileContents.Length > 0, "Generated PDF should not be empty.");
    }

    [Test]
    public void Download_ShouldReturnNull_WhenNoDataAvailable()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
             .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
             .Options;

        _dbContext = new ApplicationDbContext(options);

        // Mock other dependencies
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(env => env.ContentRootPath).Returns(AppDomain.CurrentDomain.BaseDirectory);

        _memoryCacheMock = new Mock<IMemoryCache>();

        // Set up the memory cache mock to return false for TryGetValue
        object cachedData = null;
        _memoryCacheMock.Setup(m => m.TryGetValue("Harmonogram", out cachedData))
            .Returns(false);

        _controller = new DyzurnyController(_dbContext, _hostEnvironmentMock.Object, _memoryCacheMock.Object);


        // Act
        var result = _controller.Download();

        // Assert
        Assert.IsNull(result, "Expected null result when no data is available.");
    }

    [Test]
    public void Download_ShouldReturnPdfFile_WhenDataIsAvailable()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
             .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
             .Options;

        _dbContext = new ApplicationDbContext(options);

        // Seed data if necessary
        _dbContext.Harmonogramy.Add(new Harmonogram
        {
            ID_Harmonogram = 1,
            ID_Zolnierza = 2,
            Data = DateTime.Now,
            Miesiac = DateTime.Now.Month.ToString(),
            Zolnierz = new Zolnierz { Imie = "John", Nazwisko = "Doe", Stopien = "kapral", Pesel = "01234567891011", ImieOjca = "Marek" },
            Sluzba = new Sluzba { ID_Sluzby = 1, Przelozony = "oficer dyzurny", MiejscePelnieniaSluzby = "Wartownia", Rodzaj = "Guard Duty" }
        });
        _dbContext.SaveChanges();

        // Mock other dependencies
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(env => env.ContentRootPath).Returns(AppDomain.CurrentDomain.BaseDirectory);

        _memoryCacheMock = new Mock<IMemoryCache>();

        _controller = new DyzurnyController(_dbContext, _hostEnvironmentMock.Object, _memoryCacheMock.Object);
        // Act
        var result = _controller.Download() as FileContentResult;

        // Assert
        Assert.IsNotNull(result, "Expected a FileContentResult.");
        Assert.AreEqual("application/pdf", result.ContentType);
        Assert.AreEqual("Harmonogram.pdf", result.FileDownloadName);
    }

    [TearDown]
    public void TearDown()
    {
        // Dispose of the controller
        _controller?.Dispose();
        _dbContext?.Dispose();
    }

    public void Dispose()
    {
        // Ensure disposal of controller in case TearDown is not called
        _controller?.Dispose();
        _dbContext?.Dispose();
    }
}
