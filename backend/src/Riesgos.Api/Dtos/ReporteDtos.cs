using Riesgos.Core.Dominio;

namespace Riesgos.Api.Dtos;

public sealed record RegistroRequest(string? Email, string? Password);

public sealed record CrearReporteRequest(string? Descripcion, string? Ubicacion, Categoria? CategoriaSugerida);

public sealed record PaginaResponse<T>(IReadOnlyList<T> Items, int Page, int Size, int Total);

public sealed record ReporteResumen(
    int Id,
    string Descripcion,
    string Ubicacion,
    EstadoReporte Estado,
    TipoReporte Tipo,
    Categoria Categoria,
    Criticidad Criticidad,
    DateTimeOffset FechaCreacion,
    string CreadorEmail);

public sealed record MarcaUsuario(DateTimeOffset Fecha, string UsuarioEmail);

public sealed record CierreResponse(DateTimeOffset Fecha, string UsuarioEmail, string AccionRealizada, string ObservacionCierre);

// Detalle común a ambos roles. El administrador recibe además ReporteDetalleAdmin; el empleado
// nunca recibe la acción sugerida ni el borrador de mail (FR-011, AC-39).
public record ReporteDetalle(
    int Id,
    string Descripcion,
    string Ubicacion,
    EstadoReporte Estado,
    TipoReporte Tipo,
    Categoria Categoria,
    Criticidad Criticidad,
    Categoria? CategoriaSugerida,
    DateTimeOffset FechaCreacion,
    string CreadorEmail,
    MarcaUsuario? InicioProgreso,
    CierreResponse? Cierre);
