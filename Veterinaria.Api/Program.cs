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

// Forzar cultura invariable para parseo consistente de números y coordenadas
var culturaInvariable = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culturaInvariable;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culturaInvariable;

// Configuración de licencia para QuestPDF (Unidad 6)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Lectura y validación de contraseña de administrador en producción (Unidad 8)
var adminPassword = builder.Configuration["Admin:Password"] ?? string.Empty;
if (!builder.Environment.IsDevelopment() && (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 8))
{
    throw new InvalidOperationException("La contraseña del administrador ('Admin:Password') debe estar configurada en producción y tener al menos 8 caracteres.");
}

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

// Configuración de CORS para desarrollo y producción (Unidad 8)
var origenApp = builder.Configuration["Cors:Origin"] ?? "http://localhost";
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsApp", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(origenApp, "http://localhost", "https://localhost", "capacitor://localhost")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
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

// Aplicar migraciones y reemplazo seguro de credenciales del administrador (Unidad 8)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
    try
    {
        await db.Database.MigrateAsync();

        if (adminPassword.Length >= 8)
        {
            var adminUser = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@veterinaria.local");
            if (adminUser != null)
            {
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
                var verifyResult = hasher.VerifyHashedPassword(adminUser, adminUser.PasswordHash, adminPassword);
                if (verifyResult != PasswordVerificationResult.Success)
                {
                    adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);
                    adminUser.ActualizadoEn = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o actualizar credenciales en la base de datos.");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("CorsApp");

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

await app.RunAsync();

