using System.Collections.Concurrent;
using Riesgos.Core.Clasificacion;

namespace Riesgos.Api.Tests.Infraestructura;

// Acumula los trabajos y los procesa solo cuando el test lo pide, para que los AC que miran
// la clasificación sean deterministas (research R1).
public sealed class ColaClasificacionManual : IColaClasificacion
{
    private readonly ConcurrentQueue<TrabajoClasificacion> _pendientes = new();

    public int Cantidad => _pendientes.Count;

    // Lo asigna el test (o un helper) con el procesador real; si es null, procesar no hace nada.
    public Func<TrabajoClasificacion, CancellationToken, Task>? Procesar { get; set; }

    public void Encolar(TrabajoClasificacion trabajo) => _pendientes.Enqueue(trabajo);

    public async Task ProcesarPendientesAsync(CancellationToken ct = default)
    {
        while (_pendientes.TryDequeue(out var trabajo))
        {
            if (Procesar is not null)
            {
                await Procesar(trabajo, ct);
            }
        }
    }
}
