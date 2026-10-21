using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Riesgos.Core.Dominio;
using Riesgos.Core.Notificacion;
using Riesgos.Core.Persistencia;

namespace Riesgos.Core.Clasificacion;

// Clasifica un reporte en segundo plano (contracts/clasificador.md). Nunca deja que una falla del
// modelo afecte al reporte: si algo sale mal, queda "Pendiente de clasificación" (Principio II).
public sealed class ProcesadorClasificacion(
    RiesgosDbContext db,
    IClasificadorReportes clasificador,
    TimeProvider tiempo,
    ILogger<ProcesadorClasificacion> logger)
{
    public static readonly TimeSpan Limite = TimeSpan.FromSeconds(10);

    public async Task ProcesarAsync(TrabajoClasificacion trabajo, CancellationToken ct)
    {
        var reporte = await db.Reportes.AsNoTracking().SingleOrDefaultAsync(r => r.Id == trabajo.ReporteId, ct);
        if (reporte is null || reporte.VersionClasificacion != trabajo.Version)
        {
            // El reporte se editó después de encolar este trabajo: vale solo la última versión.
            return;
        }

        var entrada = new EntradaClasificacion(reporte.Descripcion, reporte.Ubicacion, reporte.CategoriaSugerida);
        var inicio = tiempo.GetTimestamp();
        var resultado = await ClasificarConLimiteAsync(entrada, ct);
        var latencia = tiempo.GetElapsedTime(inicio);

        var (tipo, categoria, criticidad) = resultado is ResultadoClasificacion.Exito exito && EsValido(exito)
            ? (exito.Tipo, exito.Categoria, exito.Criticidad)
            : (TipoReporte.Pendiente, Categoria.Pendiente, Criticidad.Pendiente);

        // Actualización condicional por versión: si el creador editó mientras la IA respondía,
        // este resultado ya no corresponde y no se guarda (FR-017).
        var actualizados = await db.Reportes
            .Where(r => r.Id == trabajo.ReporteId && r.VersionClasificacion == trabajo.Version)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.Tipo, tipo)
                    .SetProperty(r => r.Categoria, categoria)
                    .SetProperty(r => r.Criticidad, criticidad),
                ct);

        if (actualizados > 0)
        {
            await RegenerarBorradorAsync(trabajo.ReporteId, ct);
        }

        // Sin el texto del reporte en el log (research R13).
        logger.LogInformation(
            "Clasificación del reporte {ReporteId}: {Resultado} en {LatenciaMs} ms{Descartado}",
            trabajo.ReporteId,
            resultado is ResultadoClasificacion.Falla falla ? $"falla ({falla.Motivo})" : "éxito",
            (long)latencia.TotalMilliseconds,
            actualizados == 0 ? " (descartada: el reporte cambió)" : string.Empty);
    }

    // El borrador refleja siempre la clasificación vigente, incluido "Pendiente" (FR-022).
    private async Task RegenerarBorradorAsync(int reporteId, CancellationToken ct)
    {
        var reporte = await db.Reportes.AsNoTracking().Include(r => r.Creador).SingleAsync(r => r.Id == reporteId, ct);
        var borrador = GeneradorBorrador.Generar(reporte, reporte.Creador.Email);
        var ahora = tiempo.GetUtcNow();
        await db.EventosNotificacion
            .Where(e => e.ReporteId == reporteId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.AsuntoBorrador, borrador.Asunto)
                    .SetProperty(e => e.CuerpoBorrador, borrador.Cuerpo)
                    .SetProperty(e => e.FechaActualizacion, ahora),
                ct);
    }

    private async Task<ResultadoClasificacion> ClasificarConLimiteAsync(EntradaClasificacion entrada, CancellationToken ct)
    {
        using var limite = new CancellationTokenSource(Limite, tiempo);
        using var combinado = CancellationTokenSource.CreateLinkedTokenSource(ct, limite.Token);
        try
        {
            // WaitAsync impone el límite aunque el clasificador ignore la cancelación.
            return await clasificador.ClasificarAsync(entrada, combinado.Token).WaitAsync(Limite, tiempo, ct);
        }
        catch (Exception e) when (e is TimeoutException || (e is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return new ResultadoClasificacion.Falla(MotivoFalla.Timeout);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "El clasificador lanzó una excepción");
            return new ResultadoClasificacion.Falla(MotivoFalla.ErrorServicio);
        }
    }

    private static bool EsValido(ResultadoClasificacion.Exito e) =>
        e.Tipo != TipoReporte.Pendiente && Enum.IsDefined(e.Tipo)
        && e.Categoria != Categoria.Pendiente && Enum.IsDefined(e.Categoria)
        && e.Criticidad != Criticidad.Pendiente && Enum.IsDefined(e.Criticidad);
}
