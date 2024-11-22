using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

var builder = WebApplication.CreateBuilder(args);

// Pobierz connection string z appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Dodaj us�ug� DbContext z MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Dodaj us�ug� kontroler�w z widokami
builder.Services.AddControllersWithViews();

// Konfiguracja uwierzytelniania
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // �cie�ka do strony logowania
        options.LogoutPath = "/Account/Logout";  // �cie�ka do wylogowania
    });

// Dodaj us�ug� sesji
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Czas wyga�ni�cia sesji
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dodaj Swagger (do test�w API)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// Middleware aplikacji
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();  // Dodaj Swagger
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // Uwierzytelnianie
app.UseAuthorization();   // Autoryzacja

app.UseSession();  // Middleware sesji

// Konfiguracja routingu MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
