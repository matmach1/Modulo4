using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
using Riesgos.Core.Servicios;

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

        return grupo;
    }
}
