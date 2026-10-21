using Riesgos.Core.Dominio;
using Riesgos.Core.Notificacion;
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

    // Se devuelve como object para que se serialice el tipo concreto según el rol (Principio III:
    // lo que el empleado no debe ver no sale del backend).
    public static object Detalle(Reporte r, UsuarioActual usuario, EventoNotificacion? evento)
    {
        if (!usuario.EsAdministrador)
        {
            return Llenar(new ReporteDetalle
            {
                Id = r.Id, Descripcion = r.Descripcion, Ubicacion = r.Ubicacion, Estado = r.Estado, Tipo = r.Tipo,
                Categoria = r.Categoria, Criticidad = r.Criticidad, FechaCreacion = r.FechaCreacion, CreadorEmail = r.Creador.Email,
            }, r);
        }

        return Llenar(new ReporteDetalleAdmin
        {
            Id = r.Id, Descripcion = r.Descripcion, Ubicacion = r.Ubicacion, Estado = r.Estado, Tipo = r.Tipo,
            Categoria = r.Categoria, Criticidad = r.Criticidad, FechaCreacion = r.FechaCreacion, CreadorEmail = r.Creador.Email,
            AccionSugerida = AccionSugerida.Para(r.Criticidad),
            BorradorMail = evento is null ? null : new BorradorMailResponse(evento.AsuntoBorrador, evento.CuerpoBorrador),
        }, r);
    }

    private static T Llenar<T>(T detalle, Reporte r)
        where T : ReporteDetalle => detalle with
        {
            // Ambas categorías quedan visibles cuando difieren (FR-016, FR-025).
            CategoriaSugerida = r.CategoriaSugerida,
            InicioProgreso = r.FechaInicioProgreso is { } inicio && r.UsuarioInicioProgreso is { } quienInicio
                ? new MarcaUsuario(inicio, quienInicio.Email)
                : null,
            Cierre = r.FechaCierre is { } cierre && r.UsuarioCierre is { } quienCierra
                ? new CierreResponse(cierre, quienCierra.Email, r.AccionRealizada!, r.ObservacionCierre!)
                : null,
            Acciones = r.Acciones
                .OrderBy(a => a.Fecha).ThenBy(a => a.Id)
                .Select(Accion)
                .ToList(),
        };

    public static AccionResponse Accion(AccionCorrectiva a) => new(a.Id, a.Descripcion, a.Fecha, a.Responsable.Email);
}
