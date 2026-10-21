namespace Riesgos.Core.Dominio;

// Acción correctiva o preventiva (un único concepto, FR-028). No se edita ni se elimina.
public class AccionCorrectiva
{
    public int Id { get; set; }

    public int ReporteId { get; set; }

    public Reporte Reporte { get; set; } = null!;

    public required string Descripcion { get; set; }

    public DateTimeOffset Fecha { get; set; }

    public int ResponsableId { get; set; }

    public Usuario Responsable { get; set; } = null!;
}
