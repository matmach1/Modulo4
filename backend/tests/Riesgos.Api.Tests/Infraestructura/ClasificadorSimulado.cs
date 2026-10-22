using System.Collections.Concurrent;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.Infraestructura;

// Reemplaza a Claude API en todos los tests (Principio IV): nunca se llama a la API real.
public sealed class ClasificadorSimulado : IClasificadorReportes
{
    private readonly ConcurrentQueue<EntradaClasificacion> _entradas = new();

    public IReadOnlyCollection<EntradaClasificacion> Entradas => _entradas.ToArray();

    // Resultado que devuelve la próxima llamada; por defecto, una clasificación válida.
    public ResultadoClasificacion Resultado { get; set; } =
        new ResultadoClasificacion.Exito(TipoReporte.CondicionInsegura, Categoria.RiesgoElectrico, Criticidad.Alta);

    // Si es true, la llamada espera hasta que el procesador la cancele (simula falta de respuesta).
    public bool NoResponder { get; set; }

    public async Task<ResultadoClasificacion> ClasificarAsync(EntradaClasificacion entrada, CancellationToken ct)
    {
        _entradas.Enqueue(entrada);
        if (NoResponder)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                return new ResultadoClasificacion.Falla(MotivoFalla.Timeout);
            }
        }

        return Resultado;
    }
}
