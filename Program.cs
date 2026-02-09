using bot.Data;
using bot.Handlers;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Connection String'ni olish va parse qilish
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = ParseConnectionString(rawConnectionString);

Console.WriteLine($"📊 Connection String Format: {(connectionString.Contains("Host=") ? "Npgsql" : "Unknown")}");

// Database context
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Host="))
{
    Console.WriteLine("🐘 Using PostgreSQL");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    Console.WriteLine("📁 Using SQLite");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=app.db"));
}

// Command Handlers
builder.Services.AddScoped<ICommandHandler, StartCommandHandler>();

// Bot Service
builder.Services.AddHostedService<TelegramBotService>();

var app = builder.Build();

// Auto migration in production
if (builder.Environment.IsProduction())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        Console.WriteLine("🔄 Running migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("✅ Database migrated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// ✅ PostgreSQL URI'ni Npgsql formatiga o'girish funksiyasi
static string ParseConnectionString(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        return "Data Source=app.db"; // Default SQLite
    }

    // Agar Npgsql formatda bo'lsa, o'zgartirishsiz qaytaramiz
    if (connectionString.Contains("Host="))
    {
        return connectionString;
    }

    // Agar PostgreSQL URI formatda bo'lsa, parse qilamiz
    if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
    {
        try
        {
            // Regex: postgresql://user:password@host:port/database?params
            var pattern = @"postgres(?:ql)?://(?<user>[^:]+):(?<password>[^@]+)@(?<host>[^:]+):(?<port>\d+)/(?<database>[^\?]+)(\?(?<params>.+))?";
            var regex = new Regex(pattern);
            var match = regex.Match(connectionString);

            if (!match.Success)
            {
                Console.WriteLine($"⚠️ Failed to parse PostgreSQL URI, using as-is");
                return connectionString;
            }

            var user = Uri.UnescapeDataString(match.Groups["user"].Value);
            var password = Uri.UnescapeDataString(match.Groups["password"].Value);
            var host = match.Groups["host"].Value;
            var port = match.Groups["port"].Value;
            var database = Uri.UnescapeDataString(match.Groups["database"].Value);
            var paramsStr = match.Groups["params"].Value;

            // SSL Mode'ni parse qilish
            var sslMode = "Require";
            if (!string.IsNullOrEmpty(paramsStr))
            {
                var parameters = paramsStr.Split('&');
                foreach (var param in parameters)
                {
                    var parts = param.Split('=');
                    if (parts.Length >= 1 && parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                    {
                        // Agar sslmode= (qiymatsiz) bo'lsa, default ishlatamiz
                        if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
                        {
                            sslMode = parts[1];
                        }
                        break;
                    }
                }
            }

            // Npgsql formatiga o'girish
            var npgsqlConnectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";

            Console.WriteLine($"✅ Parsed PostgreSQL URI successfully");
            Console.WriteLine($"   Host: {host}");
            Console.WriteLine($"   Port: {port}");
            Console.WriteLine($"   Database: {database}");
            Console.WriteLine($"   Username: {user}");
            Console.WriteLine($"   SSL Mode: {sslMode}");

            return npgsqlConnectionString;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error parsing PostgreSQL URI: {ex.Message}");
            return connectionString;
        }
    }

    // Agar SQLite yoki boshqa format bo'lsa
    return connectionString;
}