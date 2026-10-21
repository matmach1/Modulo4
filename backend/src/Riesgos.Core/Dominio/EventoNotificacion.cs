namespace Riesgos.Core.Dominio;

// Registro de que se creó un reporte, con el borrador de mail para el administrador (FR-021).
// El borrador se reemplaza cada vez que cambia la clasificación (FR-022); no se guarda historial.
public class EventoNotificacion
{
    public int Id { get; set; }

    public int ReporteId { get; set; }

    public Reporte Reporte { get; set; } = null!;

    public DateTimeOffset FechaCreacion { get; set; }

    public required string AsuntoBorrador { get; set; }

    public required string CuerpoBorrador { get; set; }

    public DateTimeOffset FechaActualizacion { get; set; }
}
