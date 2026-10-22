using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth");

        grupo.MapPost("/registro", async (RegistroRequest request, ServicioCuentas cuentas, CancellationToken ct) =>
        {
            var usuario = await cuentas.RegistrarAsync(request.Email, request.Password, ct);
            return Results.Created($"/api/auth/yo", new UsuarioResponse(usuario.Id, usuario.Email, usuario.Rol));
        });

        grupo.MapPost("/login", async (LoginRequest request, ServicioCuentas cuentas, ServicioSesiones sesiones, HttpContext http, ILoggerFactory logs, CancellationToken ct) =>
        {
            var usuario = await cuentas.AutenticarAsync(request.Email, request.Password, ct);
            if (usuario is null)
            {
                // Evento de seguridad sin email ni contraseña (research R13).
                logs.CreateLogger("Riesgos.Seguridad").LogWarning("Inicio de sesión fallido desde {Ip}", http.Connection.RemoteIpAddress);
                // Mismo mensaje para email inexistente y contraseña incorrecta (research R4).
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email o contraseña incorrectos.");
            }

            var token = await sesiones.CrearAsync(usuario.Id, ct);
            http.Response.Cookies.Append(SesionAuthenticationHandler.Cookie, token, OpcionesCookie());
            return Results.Ok(new UsuarioResponse(usuario.Id, usuario.Email, usuario.Rol));
        });

        // Invalida la sesión en el servidor de inmediato (FR-005a, RF-42) y expira la cookie.
        grupo.MapPost("/logout", async (ServicioSesiones sesiones, HttpContext http, CancellationToken ct) =>
        {
            await sesiones.CerrarAsync(http.User.TokenSesion(), ct);
            http.Response.Cookies.Delete(SesionAuthenticationHandler.Cookie, OpcionesCookie());
            return Results.NoContent();
        }).RequireAuthorization();

        grupo.MapGet("/yo", (HttpContext http) =>
        {
            var usuario = http.User.UsuarioActual();
            return Results.Ok(new UsuarioResponse(usuario.Id, usuario.Email, usuario.Rol));
        }).RequireAuthorization();

        grupo.MapPut("/password", async (CambiarPasswordRequest request, ServicioCuentas cuentas, HttpContext http, CancellationToken ct) =>
        {
            await cuentas.CambiarPasswordAsync(http.User.UsuarioActual(), http.User.TokenSesion(), request.PasswordActual, request.PasswordNueva, ct);
            return Results.NoContent();
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
