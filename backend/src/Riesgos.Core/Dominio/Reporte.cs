namespace Riesgos.Core.Dominio;

public class Reporte
{
    public const int MaxDescripcion = 2000;
    public const int MaxUbicacion = 200;

    public int Id { get; set; }

    public required string Descripcion { get; set; }

    public required string Ubicacion { get; set; }

    // Solo se fija al crear (el PRD excluye editarla). Nunca es Pendiente.
    public Categoria? CategoriaSugerida { get; set; }

    public TipoReporte Tipo { get; set; } = TipoReporte.Pendiente;

    // Categoría definitiva: siempre la automática (FR-016).
    public Categoria Categoria { get; set; } = Categoria.Pendiente;

    public Criticidad Criticidad { get; set; } = Criticidad.Pendiente;

    // Se incrementa en cada creación o edición; un resultado de otra versión se descarta (FR-017).
    public int VersionClasificacion { get; set; }

    public EstadoReporte Estado { get; set; } = EstadoReporte.Abierto;

    public int CreadorId { get; set; }

    public Usuario Creador { get; set; } = null!;

    public DateTimeOffset FechaCreacion { get; set; }

    public DateTimeOffset? FechaInicioProgreso { get; set; }

    public int? UsuarioInicioProgresoId { get; set; }

    public Usuario? UsuarioInicioProgreso { get; set; }

    public string? AccionRealizada { get; set; }

    public string? ObservacionCierre { get; set; }

    public DateTimeOffset? FechaCierre { get; set; }

    public int? UsuarioCierreId { get; set; }

    public Usuario? UsuarioCierre { get; set; }

    // Token de concurrencia optimista (research R7).
    public int RowVersion { get; set; }

    public void ReiniciarClasificacion()
    {
        Tipo = TipoReporte.Pendiente;
        Categoria = Categoria.Pendiente;
        Criticidad = Criticidad.Pendiente;
        VersionClasificacion++;
    }

    // Esqueleto: se implementa en T079.
    public void CambiarEstado(EstadoReporte destino, int adminId, DateTimeOffset ahora, string? accionRealizada = null, string? observacionCierre = null) =>
        throw new NotImplementedException();
}
