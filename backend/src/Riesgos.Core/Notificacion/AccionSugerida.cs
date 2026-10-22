using Riesgos.Core.Dominio;

namespace Riesgos.Core.Notificacion;

// Acción preventiva predefinida según la criticidad (FR-020); solo se muestra al administrador.
public static class AccionSugerida
{
    public static string? Para(Criticidad criticidad) => criticidad switch
    {
        Criticidad.Critica => "Detener actividad",
        Criticidad.Alta => "Requiere intervención inmediata",
        _ => null,
    };
}
