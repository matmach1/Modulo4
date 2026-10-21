using Microsoft.Extensions.Logging;
using Riesgos.Core.Persistencia;

namespace Riesgos.Core.Clasificacion;

public sealed class ProcesadorClasificacion(
    RiesgosDbContext db,
    IClasificadorReportes clasificador,
    TimeProvider tiempo,
    ILogger<ProcesadorClasificacion> logger)
{
    // Esqueleto: se implementa en T052.
    public Task ProcesarAsync(TrabajoClasificacion trabajo, CancellationToken ct)
    {
        _ = (db, clasificador, tiempo, logger);
        throw new NotImplementedException();
    }
}
