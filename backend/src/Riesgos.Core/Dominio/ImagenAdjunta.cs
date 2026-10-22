namespace Riesgos.Core.Dominio;

// Evidencia visual para el administrador; nunca se envía al clasificador (FR-015, RF-21).
public class ImagenAdjunta
{
    public const int MaximoPorReporte = 5;

    public int Id { get; set; }

    public int ReporteId { get; set; }

    public Reporte Reporte { get; set; } = null!;

    // Nombre generado (GUID + extensión): el nombre original nunca se usa en rutas.
    public required string NombreArchivo { get; set; }

    public required string TipoContenido { get; set; }

    public int TamanoBytes { get; set; }

    public DateTimeOffset FechaCarga { get; set; }
}
