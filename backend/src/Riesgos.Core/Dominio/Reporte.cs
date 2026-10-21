using Riesgos.Core.Validacion;

namespace Riesgos.Core.Dominio;

public class Reporte
{
    public const int MaxDescripcion = 2000;
    public const int MaxUbicacion = 200;
    public const int MaxTextoGestion = 1000;

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

    // Único flujo válido: Abierto → En Progreso → Cerrado (FR-027). Cualquier otra transición,
    // incluida al mismo estado, es un conflicto de estado. La clasificación no la condiciona.
    public void CambiarEstado(EstadoReporte destino, int adminId, DateTimeOffset ahora, string? accionRealizada = null, string? observacionCierre = null)
    {
        switch (Estado, destino)
        {
            case (EstadoReporte.Abierto, EstadoReporte.EnProgreso):
                Estado = EstadoReporte.EnProgreso;
                FechaInicioProgreso = ahora;
                UsuarioInicioProgresoId = adminId;
                break;

            case (EstadoReporte.EnProgreso, EstadoReporte.Cerrado):
                // Se valida todo antes de modificar: si falta un texto, el reporte sigue En Progreso.
                var accion = Texto.Validar(accionRealizada, "accionRealizada", MaxTextoGestion);
                var observacion = Texto.Validar(observacionCierre, "observacionCierre", MaxTextoGestion);
                Estado = EstadoReporte.Cerrado;
                AccionRealizada = accion;
                ObservacionCierre = observacion;
                FechaCierre = ahora;
                UsuarioCierreId = adminId;
                break;

            default:
                throw new Conflicto($"No se puede pasar un reporte de {Estado} a {destino}.");
        }
    }
}
