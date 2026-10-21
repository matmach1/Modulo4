using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Riesgos.Api.Auth;
using Riesgos.Api.Endpoints;
using Riesgos.Api.Infraestructura;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Servicios;

var builder = WebApplication.CreateBuilder(args);

// La configuración se lee al resolver los servicios (no acá), para que los tests puedan
// reemplazarla con UseSetting.
builder.Services.AddDbContext<RiesgosDbContext>((sp, opciones) =>
    opciones.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Riesgos") ?? "Data Source=data/riesgos.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new HashContrasena(
    sp.GetRequiredService<IConfiguration>().GetValue("Seguridad:FactorBcrypt", 12)));
builder.Services.AddScoped<ServicioSesiones>();
builder.Services.AddScoped<ServicioCuentas>();

builder.Services.AddSingleton<ColaClasificacion>();
builder.Services.AddSingleton<IColaClasificacion>(sp => sp.GetRequiredService<ColaClasificacion>());

builder.Services.AddAutenticacionPorSesion();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// FR-032: HTTPS obligatorio fuera de desarrollo. HTTPS_PORT es opcional (si no, se detecta).
builder.Services.AddHttpsRedirection(o => o.HttpsPort = builder.Configuration.GetValue<int?>("HTTPS_PORT"));

var app = builder.Build();

await PrepararBaseAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.Run();

static async Task PrepararBaseAsync(IServiceProvider servicios)
{
    using (var scope = servicios.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RiesgosDbContext>();
        var origen = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var carpeta = Path.GetDirectoryName(Path.GetFullPath(origen));
        if (!string.IsNullOrEmpty(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        await db.Database.MigrateAsync();
    }

    await SeedAdministrador.EjecutarAsync(servicios);
}

public partial class Program;
