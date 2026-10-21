using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;

namespace Riesgos.Core.Tests;

// Base SQLite en un archivo temporal con las migraciones aplicadas; cada contexto es independiente.
public sealed class BaseDePrueba : IDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"riesgos-core-{Guid.NewGuid():N}.db");

    public BaseDePrueba()
    {
        using var db = NuevoContexto();
        db.Database.Migrate();
    }

    public RiesgosDbContext NuevoContexto() =>
        new(new DbContextOptionsBuilder<RiesgosDbContext>().UseSqlite($"Data Source={_ruta}").Options);

    public async Task<Reporte> CrearReporteAsync(string descripcion = "Cable pelado", string ubicacion = "Planta 1", Categoria? sugerida = null)
    {
        await using var db = NuevoContexto();
        var usuario = new Usuario { Email = $"{Guid.NewGuid():N}@riesgos.test", PasswordHash = "x", Rol = Rol.Empleado };
        var reporte = new Reporte { Descripcion = descripcion, Ubicacion = ubicacion, CategoriaSugerida = sugerida, Creador = usuario };
        reporte.ReiniciarClasificacion();
        db.Reportes.Add(reporte);
        await db.SaveChangesAsync();
        db.EventosNotificacion.Add(new EventoNotificacion { ReporteId = reporte.Id, AsuntoBorrador = "inicial", CuerpoBorrador = "inicial" });
        await db.SaveChangesAsync();
        return reporte;
    }

    public async Task<Reporte> LeerAsync(int id)
    {
        await using var db = NuevoContexto();
        return await db.Reportes.AsNoTracking().SingleAsync(r => r.Id == id);
    }

    public async Task<EventoNotificacion> LeerEventoAsync(int reporteId)
    {
        await using var db = NuevoContexto();
        return await db.EventosNotificacion.AsNoTracking().SingleAsync(e => e.ReporteId == reporteId);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_ruta);
    }
}
