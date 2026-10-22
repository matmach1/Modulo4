using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Riesgos.Core.Clasificacion;

namespace Riesgos.Api.Tests.Infraestructura;

// Cada instancia usa su propia base SQLite en un archivo temporal: aísla los tests y permite
// probar concurrencia real (varias conexiones), cosa que una base en memoria compartida no ofrece.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@riesgos.test";
    public const string AdminPassword = "Admin12345";

    private readonly string _rutaDb = Path.Combine(Path.GetTempPath(), $"riesgos-tests-{Guid.NewGuid():N}.db");

    public string RutaImagenes { get; } = Path.Combine(Path.GetTempPath(), $"riesgos-img-{Guid.NewGuid():N}");

    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));

    public ClasificadorSimulado Clasificador { get; } = new();

    public ColaClasificacionManual Cola { get; } = new();

    public string Entorno { get; init; } = "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Entorno);
        builder.UseSetting("ConnectionStrings:Riesgos", $"Data Source={_rutaDb}");
        builder.UseSetting("ADMIN_EMAIL", AdminEmail);
        builder.UseSetting("ADMIN_PASSWORD", AdminPassword);
        builder.UseSetting("Seguridad:FactorBcrypt", "4");
        builder.UseSetting("Imagenes:Ruta", RutaImagenes);
        builder.UseSetting("HTTPS_PORT", "443");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(Reloj);
            servicios.RemoveAll<IClasificadorReportes>();
            servicios.AddSingleton<IClasificadorReportes>(Clasificador);
            servicios.RemoveAll<IColaClasificacion>();
            servicios.AddSingleton<IColaClasificacion>(Cola);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_rutaDb);
            if (Directory.Exists(RutaImagenes))
            {
                Directory.Delete(RutaImagenes, recursive: true);
            }
        }
        catch (IOException)
        {
            // Archivos temporales: si quedan bloqueados no afecta el resultado del test.
        }
    }
}
