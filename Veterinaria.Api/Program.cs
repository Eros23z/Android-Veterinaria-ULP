using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Veterinaria.Api.Auth;
using Veterinaria.Api.Data;
using Veterinaria.Api.Domain.Entities;

// Configuración de licencia para QuestPDF (Unidad 6)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Configuración de JSON con snake_case
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    });

// Inyección de servicios de autenticación y seguridad
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// Configuración de Autenticación con JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveEfimeraVeterinariaSanRoque2026SuperSeguraDesarrollo12345!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Veterinaria.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Veterinaria.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<VeterinariaDbContext>();
                var subClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (!long.TryParse(subClaim, out var usuarioId))
                {
                    context.Fail("Token inválido: falta claim sub.");
                    return;
                }

                var usuario = await db.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == usuarioId);

                if (usuario == null || !usuario.Activo)
                {
                    context.Fail("Usuario inactivo o no encontrado.");
                    return;
                }

                var tokenRol = context.Principal?.FindFirst("rol_codigo")?.Value
                            ?? context.Principal?.FindFirst(ClaimTypes.Role)?.Value;

                var rolActual = usuario.Rol?.Codigo;

                if (tokenRol != rolActual)
                {
                    context.Fail("El rol del usuario ha cambiado.");
                    return;
                }
            },
        };
    });

builder.Services.AddAuthorization();

// Configuración de CORS para desarrollo y Capacitor (WebView)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodoDesarrollo", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configuración de DbContext con SQL Server y convenciones snake_case
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.\\SQLEXPRESS;Database=VeterinariaDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<VeterinariaDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .UseSnakeCaseNamingConvention();
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // En desarrollo, aplicar migraciones pendientes automáticamente al iniciar
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones en la base de datos.");
    }
}

app.UseCors("PermitirTodoDesarrollo");

// Asegurar existencia de directorio para uploads de clientes
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads", "clientes");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Servir archivos estáticos desde wwwroot (uploads, seed, etc.)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

