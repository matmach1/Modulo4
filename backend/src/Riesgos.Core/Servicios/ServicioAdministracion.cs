using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;

namespace Riesgos.Core.Servicios;

public sealed record Panel(int CriticosAbiertos, int PendientesAbiertos);

// Operaciones exclusivas del administrador (FR-030); la API las protege con la política de rol.
public sealed class ServicioAdministracion(RiesgosDbContext db)
{
    public async Task<Pagina<Reporte>> ListarAsync(int? page, int? size, CancellationToken ct = default)
    {
        var (p, s) = Paginacion.Validar(page, size);
        var total = await db.Reportes.CountAsync(ct);
        var items = await db.Reportes.AsNoTracking()
            .Include(r => r.Creador)
            .OrderByDescending(r => r.FechaCreacion).ThenByDescending(r => r.Id)
            .Skip((p - 1) * s).Take(s)
            .ToListAsync(ct);
        return new Pagina<Reporte>(items, p, s, total);
    }

    // Solo reportes en estado Abierto; En Progreso y Cerrado no cuentan (FR-026).
    public async Task<Panel> PanelAsync(CancellationToken ct = default)
    {
        var abiertos = db.Reportes.Where(r => r.Estado == EstadoReporte.Abierto);
        return new Panel(
            await abiertos.CountAsync(r => r.Criticidad == Criticidad.Critica, ct),
            await abiertos.CountAsync(r => r.Criticidad == Criticidad.Pendiente, ct));
    }
}
