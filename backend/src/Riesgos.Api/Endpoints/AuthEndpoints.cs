using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth");

        grupo.MapPost("/login", async (LoginRequest request, ServicioCuentas cuentas, ServicioSesiones sesiones, HttpContext http, CancellationToken ct) =>
        {
            var usuario = await cuentas.AutenticarAsync(request.Email, request.Password, ct);
            if (usuario is null)
            {
                // Mismo mensaje para email inexistente y contraseña incorrecta (research R4).
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email o contraseña incorrectos.");
            }

            var token = await sesiones.CrearAsync(usuario.Id, ct);
            http.Response.Cookies.Append(SesionAuthenticationHandler.Cookie, token, OpcionesCookie());
            return Results.Ok(new UsuarioResponse(usuario.Id, usuario.Email, usuario.Rol));
        });

        grupo.MapGet("/yo", (HttpContext http) =>
        {
            var usuario = http.User.UsuarioActual();
            return Results.Ok(new UsuarioResponse(usuario.Id, usuario.Email, usuario.Rol));
        }).RequireAuthorization();

        return grupo;
    }

    // La sesión la controla el servidor (24 h de inactividad): la cookie no lleva vencimiento propio.
    internal static CookieOptions OpcionesCookie() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
    };
}
