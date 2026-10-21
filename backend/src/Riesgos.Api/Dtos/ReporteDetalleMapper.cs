using Riesgos.Core.Dominio;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Dtos;

public static class ReporteDetalleMapper
{
    private const int LargoResumen = 140;

    public static ReporteResumen Resumen(Reporte r) => new(
        r.Id,
        r.Descripcion.Length > LargoResumen ? r.Descripcion[..LargoResumen] + "…" : r.Descripcion,
        r.Ubicacion,
        r.Estado,
        r.Tipo,
        r.Categoria,
        r.Criticidad,
        r.FechaCreacion,
        r.Creador.Email);

    public static PaginaResponse<ReporteResumen> Pagina(Pagina<Reporte> pagina) =>
        new(pagina.Items.Select(Resumen).ToList(), pagina.Page, pagina.Size, pagina.Total);

    // Se devuelve como object para que se serialice el tipo concreto según el rol.
    public static object Detalle(Reporte r, UsuarioActual usuario) => Comun(r);

    private static ReporteDetalle Comun(Reporte r) => new(
        r.Id,
        r.Descripcion,
        r.Ubicacion,
        r.Estado,
        r.Tipo,
        r.Categoria,
        r.Criticidad,
        r.CategoriaSugerida,
        r.FechaCreacion,
        r.Creador.Email,
        r.FechaInicioProgreso is { } inicio && r.UsuarioInicioProgreso is { } quienInicio
            ? new MarcaUsuario(inicio, quienInicio.Email)
            : null,
        r.FechaCierre is { } cierre && r.UsuarioCierre is { } quienCierra
            ? new CierreResponse(cierre, quienCierra.Email, r.AccionRealizada!, r.ObservacionCierre!)
            : null);
}
