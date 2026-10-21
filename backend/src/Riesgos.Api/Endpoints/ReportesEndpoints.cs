using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
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
            return Results.Created($"/api/reportes/{reporte.Id}", ReporteDetalleMapper.Detalle(reporte, usuario));
        });

        grupo.MapGet("/mios", async (int? page, int? size, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
            Results.Ok(ReporteDetalleMapper.Pagina(await reportes.ListarPropiosAsync(http.User.UsuarioActual(), page, size, ct))));

        grupo.MapGet("/{id:int}", async (int id, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
        {
            var usuario = http.User.UsuarioActual();
            return Results.Ok(ReporteDetalleMapper.Detalle(await reportes.ObtenerDetalleAsync(id, usuario, ct), usuario));
        });

        return grupo;
    }
}
