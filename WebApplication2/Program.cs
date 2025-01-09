using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

var builder = WebApplication.CreateBuilder(args);

//Jedrych tu byl
// Pobierz connection string z appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Dodaj us�ug� DbContext z MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Dodaj us�ug� kontroler�w z widokami
builder.Services.AddControllersWithViews();

// Konfiguracja uwierzytelniania z dwoma schematami
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";    // �cie�ka do logowania u�ytkownik�w
    options.LogoutPath = "/Account/Logout";  // �cie�ka do wylogowania u�ytkownik�w
})
.AddCookie("AdminScheme", options =>
{
    options.LoginPath = "/AdminLogin/Login";    // �cie�ka do logowania administrator�w
    options.LogoutPath = "/AdminLogout/Logout";  // �cie�ka do wylogowania administrator�w
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
