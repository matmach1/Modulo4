using System.Threading.Channels;

namespace Riesgos.Core.Clasificacion;

// Cola en memoria (research R1): si la app se reinicia, los trabajos pendientes se pierden y esos
// reportes quedan "Pendiente de clasificación", sin reintento, como pide la spec.
public sealed class ColaClasificacion : IColaClasificacion
{
    private readonly Channel<TrabajoClasificacion> _canal = Channel.CreateUnbounded<TrabajoClasificacion>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<TrabajoClasificacion> Lector => _canal.Reader;

    public void Encolar(TrabajoClasificacion trabajo) => _canal.Writer.TryWrite(trabajo);
}
