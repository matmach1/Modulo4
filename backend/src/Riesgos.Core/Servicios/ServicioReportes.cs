using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;
using Riesgos.Core.Notificacion;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Servicios;

public sealed class ServicioReportes(RiesgosDbContext db, IColaClasificacion cola, TimeProvider tiempo)
{
    // Valida antes de todo (FR-019): un reporte inválido no llega a pedir clasificación.
    public async Task<Reporte> CrearAsync(UsuarioActual usuario, string? descripcion, string? ubicacion, Categoria? categoriaSugerida, CancellationToken ct = default)
    {
        var reporte = new Reporte
        {
            Descripcion = Texto.Validar(descripcion, "descripcion", Reporte.MaxDescripcion),
            Ubicacion = Texto.Validar(ubicacion, "ubicacion", Reporte.MaxUbicacion),
            CategoriaSugerida = ValidarCategoriaSugerida(categoriaSugerida),
            CreadorId = usuario.Id,
            FechaCreacion = tiempo.GetUtcNow(),
        };
        reporte.ReiniciarClasificacion();

        // Reporte y evento de notificación en la misma transacción (FR-021, AC-07). El asunto del
        // borrador lleva el número del reporte, así que primero se guarda el reporte.
        await using (var transaccion = await db.Database.BeginTransactionAsync(ct))
        {
            db.Reportes.Add(reporte);
            await db.SaveChangesAsync(ct);
            var borrador = GeneradorBorrador.Generar(reporte, usuario.Email);
            db.EventosNotificacion.Add(new EventoNotificacion
            {
                ReporteId = reporte.Id,
                FechaCreacion = reporte.FechaCreacion,
                FechaActualizacion = reporte.FechaCreacion,
                AsuntoBorrador = borrador.Asunto,
                CuerpoBorrador = borrador.Cuerpo,
            });
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }

        // Se clasifica en segundo plano: la respuesta no espera a la IA (FR-014).
        cola.Encolar(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));
        return await ObtenerDetalleAsync(reporte.Id, usuario, ct);
    }

    // Solo el creador y solo en Abierto (FR-012). Pone la clasificación en pendiente, regenera el
    // borrador y encola una nueva clasificación sin esperarla (FR-017, FR-022).
    public async Task<Reporte> EditarAsync(int id, UsuarioActual usuario, string? descripcion, string? ubicacion, CancellationToken ct = default)
    {
        var reporte = await db.Reportes.SingleOrDefaultAsync(r => r.Id == id, ct) ?? throw new NoEncontrado("El reporte no existe.");
        if (reporte.CreadorId != usuario.Id)
        {
            throw new AccesoDenegado("Solo quien creó el reporte puede editarlo.");
        }

        if (reporte.Estado != EstadoReporte.Abierto)
        {
            throw new Conflicto("Solo se puede editar un reporte en estado Abierto.");
        }

        reporte.Descripcion = Texto.Validar(descripcion, "descripcion", Reporte.MaxDescripcion);
        reporte.Ubicacion = Texto.Validar(ubicacion, "ubicacion", Reporte.MaxUbicacion);
        reporte.ReiniciarClasificacion();

        var evento = await db.EventosNotificacion.SingleOrDefaultAsync(e => e.ReporteId == id, ct);
        if (evento is not null)
        {
            var borrador = GeneradorBorrador.Generar(reporte, usuario.Email);
            evento.AsuntoBorrador = borrador.Asunto;
            evento.CuerpoBorrador = borrador.Cuerpo;
            evento.FechaActualizacion = tiempo.GetUtcNow();
        }

        await db.SaveChangesAsync(ct);
        cola.Encolar(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));
        return await ObtenerDetalleAsync(reporte.Id, usuario, ct);
    }

    public async Task<Pagina<Reporte>> ListarPropiosAsync(UsuarioActual usuario, int? page, int? size, CancellationToken ct = default)
    {
        var (p, s) = Paginacion.Validar(page, size);
        var consulta = db.Reportes.AsNoTracking().Where(r => r.CreadorId == usuario.Id);
        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .Include(r => r.Creador)
            .OrderByDescending(r => r.FechaCreacion).ThenByDescending(r => r.Id)
            .Skip((p - 1) * s).Take(s)
            .ToListAsync(ct);
        return new Pagina<Reporte>(items, p, s, total);
    }

    // Solo el creador o el administrador pueden ver un reporte (FR-011, FR-025, AC-04).
    public async Task<Reporte> ObtenerDetalleAsync(int id, UsuarioActual usuario, CancellationToken ct = default)
    {
        var reporte = await db.Reportes.AsNoTracking()
            .Include(r => r.Creador)
            .Include(r => r.UsuarioInicioProgreso)
            .Include(r => r.UsuarioCierre)
            .Include(r => r.Acciones).ThenInclude(a => a.Responsable)
            .Include(r => r.Imagenes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NoEncontrado("El reporte no existe.");
        VerificarAcceso(reporte, usuario);
        return reporte;
    }

    // El borrador de mail es solo para el administrador (FR-023).
    public async Task<EventoNotificacion?> ObtenerEventoAsync(int reporteId, UsuarioActual usuario, CancellationToken ct = default) =>
        usuario.EsAdministrador
            ? await db.EventosNotificacion.AsNoTracking().SingleOrDefaultAsync(e => e.ReporteId == reporteId, ct)
            : null;

    public static void VerificarAcceso(Reporte reporte, UsuarioActual usuario)
    {
        if (!usuario.EsAdministrador && reporte.CreadorId != usuario.Id)
        {
            throw new AccesoDenegado("No tenés permiso para ver este reporte.");
        }
    }

    private static Categoria? ValidarCategoriaSugerida(Categoria? categoria)
    {
        if (categoria is Categoria.Pendiente || (categoria is not null && !Enum.IsDefined(categoria.Value)))
        {
            throw new ErrorValidacion("categoriaSugerida", "La categoría sugerida no es válida.");
        }

        return categoria;
    }
}
