using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Models;
using SalesIntelligence.Api.Services;

// Load .env file if present at project root
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, val);
            }
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// 1. Add DB Context (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=sales_intelligence.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Register Application Services & HttpClient
builder.Services.AddHttpClient<IPredictionService, PredictionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<IGroqAgentService, GroqAgentService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<IEmployeeFeatureService, EmployeeFeatureService>();
builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
builder.Services.AddScoped<IForecastService, ForecastService>();
builder.Services.AddScoped<ISqlQueryExecutor, SqlQueryExecutor>();
builder.Services.AddSingleton<ISchemaDiscoveryService, SchemaDiscoveryService>();
builder.Services.AddScoped<IRecommendationAnalysisService, RecommendationAnalysisService>();
builder.Services.AddScoped<ISqlAgentService, SqlAgentService>();
builder.Services.AddScoped<IChatService, ChatService>();

// 4. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 5. Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sales Intelligence Web API", Version = "v1" });
});

var app = builder.Build();

// Check if GROQ_API_KEY is configured and log clear warning if missing
var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
if (string.IsNullOrWhiteSpace(groqKey))
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("⚠️ WARNING: GROQ_API_KEY is empty or missing in backend .env file. Add your Groq API key to sales-intelligence-backend/.env to enable AI agent responses.");
}

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);

        // Warm schema cache after DB is seeded
        var schemaService = services.GetRequiredService<ISchemaDiscoveryService>();
        await schemaService.WarmCacheAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

