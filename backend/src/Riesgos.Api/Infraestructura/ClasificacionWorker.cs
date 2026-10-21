using Riesgos.Core.Clasificacion;

namespace Riesgos.Api.Infraestructura;

// Consume la cola en memoria y clasifica cada reporte en su propio scope (research R1).
public sealed class ClasificacionWorker(ColaClasificacion cola, IServiceScopeFactory scopes, ILogger<ClasificacionWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var trabajo in cola.Lector.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ProcesadorClasificacion>().ProcesarAsync(trabajo, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // La app se está deteniendo: el reporte queda pendiente, sin reintento.
                return;
            }
            catch (Exception e)
            {
                // Un error con un reporte no detiene el procesamiento de los siguientes.
                logger.LogError(e, "Error al procesar la clasificación del reporte {ReporteId}", trabajo.ReporteId);
            }
        }
    }
}
