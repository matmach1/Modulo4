using Riesgos.Core.Dominio;

namespace Riesgos.Core.Clasificacion;

// Principio I: el resto del sistema solo conoce esta interfaz; la integración con Claude vive en
// Riesgos.Clasificacion (contracts/clasificador.md).
public interface IClasificadorReportes
{
    // Solo texto: nunca recibe imágenes ni datos del usuario. No lanza por fallas del servicio.
    Task<ResultadoClasificacion> ClasificarAsync(EntradaClasificacion entrada, CancellationToken ct);
}

public sealed record EntradaClasificacion(string Descripcion, string Ubicacion, Categoria? CategoriaSugerida);

public abstract record ResultadoClasificacion
{
    private ResultadoClasificacion()
    {
    }

    public sealed record Exito(TipoReporte Tipo, Categoria Categoria, Criticidad Criticidad) : ResultadoClasificacion;

    public sealed record Falla(MotivoFalla Motivo) : ResultadoClasificacion;
}

public enum MotivoFalla
{
    ErrorServicio,
    Timeout,
    RespuestaInvalida,
    SinApiKey,
}
