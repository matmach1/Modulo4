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

public sealed record BorradorMailResponse(string Asunto, string Cuerpo);

// Detalle común a ambos roles. El administrador recibe ReporteDetalleAdmin; el empleado nunca
// recibe la acción sugerida ni el borrador de mail, ni siquiera vacíos (FR-011, AC-39).
public record ReporteDetalle
{
    public required int Id { get; init; }

    public required string Descripcion { get; init; }

    public required string Ubicacion { get; init; }

    public required EstadoReporte Estado { get; init; }

    public required TipoReporte Tipo { get; init; }

    public required Categoria Categoria { get; init; }

    public required Criticidad Criticidad { get; init; }

    public Categoria? CategoriaSugerida { get; init; }

    public required DateTimeOffset FechaCreacion { get; init; }

    public required string CreadorEmail { get; init; }

    public MarcaUsuario? InicioProgreso { get; init; }

    public CierreResponse? Cierre { get; init; }
}

public sealed record ReporteDetalleAdmin : ReporteDetalle
{
    public string? AccionSugerida { get; init; }

    public BorradorMailResponse? BorradorMail { get; init; }
}
