using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using WebApplication2.Services;


var builder = WebApplication.CreateBuilder(args);

// Pobierz connection string z appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Dodaj us³ugê DbContext z MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


// Dodaj us³ugê kontrolerów z widokami
builder.Services.AddControllersWithViews();

// Ta us³uga bêdzie wywo³ywana automatycznie (np. codziennie o 9:00), aby przyznawaæ punkty
builder.Services.AddHostedService<AutomatycznaPunktacja>();

// Konfiguracja uwierzytelniania z dwoma schematami
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";    // Œcie¿ka do logowania u¿ytkowników
    options.LogoutPath = "/Account/Logout";  // Œcie¿ka do wylogowania u¿ytkowników
})
.AddCookie("AdminScheme", options =>
{
    options.LoginPath = "/AdminLogin/Login";    // Œcie¿ka do logowania administratorów
    options.LogoutPath = "/AdminLogout/Logout";  // Œcie¿ka do wylogowania administratorów
});

// Dodaj us³ugê sesji
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Czas wygaœniêcia sesji
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// To ten od powiadomieñ od czasu coœ takiego. 
builder.Services.AddHostedService<NotificationService>();

// Dodaj Swagger (do testów API)
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


