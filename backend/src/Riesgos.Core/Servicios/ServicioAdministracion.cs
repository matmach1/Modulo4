using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Servicios;

public sealed record Panel(int CriticosAbiertos, int PendientesAbiertos);

// Operaciones exclusivas del administrador (FR-030); la API las protege con la política de rol.
public sealed class ServicioAdministracion(RiesgosDbContext db, TimeProvider tiempo)
{
    // Orden por defecto: fecha descendente (RF-02d). Opcional: por criticidad (RF-02e, FR-024),
    // con fecha descendente dentro de cada nivel. El Id desempata para que la paginación sea estable.
    public async Task<Pagina<Reporte>> ListarAsync(int? page, int? size, string? orden = null, CancellationToken ct = default)
    {
        var (p, s) = Paginacion.Validar(page, size);
        var porCriticidad = (orden ?? "fecha").ToLowerInvariant() switch
        {
            "fecha" => false,
            "criticidad" => true,
            _ => throw new ErrorValidacion("orden", "El orden debe ser 'fecha' o 'criticidad'."),
        };

        var total = await db.Reportes.CountAsync(ct);
        IQueryable<Reporte> consulta = db.Reportes.AsNoTracking().Include(r => r.Creador);
        consulta = porCriticidad
            ? consulta
                .OrderBy(r => r.Criticidad == Criticidad.Critica ? 0
                    : r.Criticidad == Criticidad.Alta ? 1
                    : r.Criticidad == Criticidad.Media ? 2
                    : r.Criticidad == Criticidad.Baja ? 3
                    : 4)
                .ThenByDescending(r => r.FechaCreacion).ThenByDescending(r => r.Id)
            : consulta.OrderByDescending(r => r.FechaCreacion).ThenByDescending(r => r.Id);

        var items = await consulta.Skip((p - 1) * s).Take(s).ToListAsync(ct);
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

    // Cambiar el estado no dispara una clasificación (FR-017). Si otra operación modificó el
    // reporte entre la lectura y el guardado, EF Core lanza un error de concurrencia (409).
    public async Task CambiarEstadoAsync(int reporteId, EstadoReporte destino, string? accionRealizada, string? observacionCierre, UsuarioActual admin, CancellationToken ct = default)
    {
        var reporte = await ObtenerAsync(reporteId, ct);
        reporte.CambiarEstado(destino, admin.Id, tiempo.GetUtcNow(), accionRealizada, observacionCierre);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AccionCorrectiva> RegistrarAccionAsync(int reporteId, string? descripcion, UsuarioActual admin, CancellationToken ct = default)
    {
        var reporte = await ObtenerAsync(reporteId, ct);
        if (reporte.Estado == EstadoReporte.Cerrado)
        {
            throw new Conflicto("No se pueden registrar acciones en un reporte Cerrado.");
        }

        var accion = new AccionCorrectiva
        {
            ReporteId = reporte.Id,
            Descripcion = Texto.Validar(descripcion, "descripcion", Reporte.MaxTextoGestion),
            Fecha = tiempo.GetUtcNow(),
            ResponsableId = admin.Id,
        };
        db.AccionesCorrectivas.Add(accion);
        await db.Entry(accion).Reference(x => x.Responsable).LoadAsync(ct);

        // Marca el reporte como modificado para que una acción y un cierre simultáneos choquen.
        db.Entry(reporte).Property(r => r.RowVersion).IsModified = true;
        await db.SaveChangesAsync(ct);
        return accion;
    }

    private async Task<Reporte> ObtenerAsync(int reporteId, CancellationToken ct) =>
        await db.Reportes.SingleOrDefaultAsync(r => r.Id == reporteId, ct) ?? throw new NoEncontrado("El reporte no existe.");
}
