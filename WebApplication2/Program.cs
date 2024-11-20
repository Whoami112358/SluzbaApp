using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Pobierz connection string z appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Dodaj us�ug� DbContext z MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Dodaj us�ug� kontroler�w
builder.Services.AddControllers();

// Dodaj Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// Middleware i pipeline aplikacji
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Endpoint do wykonania zapytania SELECT * FROM Studenci
app.MapGet("/", async () =>
{
    // Lista na przechowanie wynik�w zapytania
    var students = new List<object>();

    try
    {
        // Po��czenie z baz�
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Tworzymy zapytanie SQL
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Studenci;";

        // Wykonanie zapytania i odczyt wynik�w
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            students.Add(new
            {
                Id = reader["id"],
                Imie = reader["imie"],
                Nazwisko = reader["nazwisko"],
                Wiek = reader["wiek"]
            });
        }

        // Zwr�cenie wynik�w zapytania w formacie JSON
        return Results.Json(students);
    }
    catch (Exception ex)
    {
        // W przypadku b��du zwr�cenie komunikatu
        return Results.Problem($"B��d podczas zapytania do bazy danych: {ex.Message}");
    }
});

app.Run();

// Definicja DbContext dla aplikacji
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Studenci { get; set; }
}

// Model dla tabeli Studenci
public class Student
{
    public int Id { get; set; }
    public string Imie { get; set; }
    public string Nazwisko { get; set; }
    public int Wiek { get; set; }
}
