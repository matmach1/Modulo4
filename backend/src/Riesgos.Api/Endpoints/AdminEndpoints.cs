using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
using Riesgos.Core.Servicios;
using Riesgos.Core.Validacion;

namespace Riesgos.Api.Endpoints;

// Todo el grupo exige rol administrador: un empleado recibe 403 (FR-030).
public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/admin").RequireAuthorization(Politicas.Administrador);

        grupo.MapGet("/reportes", async (int? page, int? size, ServicioAdministracion admin, CancellationToken ct) =>
            Results.Ok(ReporteDetalleMapper.Pagina(await admin.ListarAsync(page, size, ct))));

        grupo.MapGet("/panel", async (ServicioAdministracion admin, CancellationToken ct) =>
            Results.Ok(await admin.PanelAsync(ct)));

        grupo.MapPost("/reportes/{id:int}/estado", async (int id, CambiarEstadoRequest request, ServicioAdministracion admin, ServicioReportes reportes, HttpContext http, CancellationToken ct) =>
        {
            var usuario = http.User.UsuarioActual();
            var destino = request.Estado ?? throw new ErrorValidacion("estado", "El estado es obligatorio.");
            await admin.CambiarEstadoAsync(id, destino, request.AccionRealizada, request.ObservacionCierre, usuario, ct);
            return Results.Ok(await ReportesEndpoints.DetalleAsync(await reportes.ObtenerDetalleAsync(id, usuario, ct), usuario, reportes, ct));
        });

        grupo.MapPost("/reportes/{id:int}/acciones", async (int id, RegistrarAccionRequest request, ServicioAdministracion admin, HttpContext http, CancellationToken ct) =>
        {
            var accion = await admin.RegistrarAccionAsync(id, request.Descripcion, http.User.UsuarioActual(), ct);
            return Results.Created($"/api/reportes/{id}", ReporteDetalleMapper.Accion(accion));
        });

        return grupo;
    }
}
