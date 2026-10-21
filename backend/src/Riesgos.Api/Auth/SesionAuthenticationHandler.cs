using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Auth;

// Valida la cookie de sesión contra las sesiones guardadas en el servidor (research R4).
public sealed class SesionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ServicioSesiones sesiones)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Esquema = "Sesion";
    public const string Cookie = "riesgos_sesion";
    public const string ClaimToken = "token_sesion";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(Cookie, out var token) || string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        var sesion = await sesiones.ValidarAsync(token, Context.RequestAborted);
        if (sesion is null)
        {
            return AuthenticateResult.Fail("Sesión inexistente o vencida.");
        }

        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Email, sesion.Usuario.Email),
                new Claim(ClaimTypes.Role, sesion.Usuario.Rol.ToString()),
                new Claim(ClaimToken, token),
            ],
            Esquema);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema));
    }
}
