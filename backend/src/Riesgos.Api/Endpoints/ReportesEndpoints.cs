using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
using Riesgos.Core.Dominio;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Endpoints;

public static class ReportesEndpoints
{
    public static RouteGroupBuilder MapReportesEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/reportes").RequireAuthorization();

        grupo.MapPost("/", async (CrearReporteRequest request, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
        {
            var usuario = http.User.UsuarioActual();
            var reporte = await reportes.CrearAsync(usuario, request.Descripcion, request.Ubicacion, request.CategoriaSugerida, ct);
            return Results.Created($"/api/reportes/{reporte.Id}", await DetalleAsync(reporte, usuario, reportes, ct));
        });

        grupo.MapGet("/mios", async (int? page, int? size, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
            Results.Ok(ReporteDetalleMapper.Pagina(await reportes.ListarPropiosAsync(http.User.UsuarioActual(), page, size, ct))));

        grupo.MapGet("/{id:int}", async (int id, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
        {
            var usuario = http.User.UsuarioActual();
            return Results.Ok(await DetalleAsync(await reportes.ObtenerDetalleAsync(id, usuario, ct), usuario, reportes, ct));
        });

        grupo.MapPut("/{id:int}", async (int id, EditarReporteRequest request, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
        {
            var usuario = http.User.UsuarioActual();
            var reporte = await reportes.EditarAsync(id, usuario, request.Descripcion, request.Ubicacion, ct);
            return Results.Ok(await DetalleAsync(reporte, usuario, reportes, ct));
        });

        return grupo;
    }

    internal static async Task<object> DetalleAsync(Reporte reporte, UsuarioActual usuario, ServicioReportes reportes, CancellationToken ct) =>
        ReporteDetalleMapper.Detalle(reporte, usuario, await reportes.ObtenerEventoAsync(reporte.Id, usuario, ct));
}
