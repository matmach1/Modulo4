namespace Riesgos.Core.Clasificacion;

public interface IColaClasificacion
{
    void Encolar(TrabajoClasificacion trabajo);
}

// La versión permite descartar el resultado de una clasificación de una versión anterior (FR-017).
public sealed record TrabajoClasificacion(int ReporteId, int Version);
