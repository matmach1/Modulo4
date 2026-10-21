using System.Globalization;
using System.Security.Claims;
using Riesgos.Core.Dominio;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Auth;

public static class Politicas
{
    public const string Administrador = "Administrador";

    public static IServiceCollection AddAutenticacionPorSesion(this IServiceCollection servicios)
    {
        servicios.AddAuthentication(SesionAuthenticationHandler.Esquema)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SesionAuthenticationHandler>(
                SesionAuthenticationHandler.Esquema, _ => { });
        servicios.AddAuthorizationBuilder()
            .AddPolicy(Administrador, p => p.RequireAuthenticatedUser().RequireRole(nameof(Rol.Administrador)));
        return servicios;
    }

    public static UsuarioActual UsuarioActual(this ClaimsPrincipal principal) => new(
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture),
        principal.FindFirstValue(ClaimTypes.Email)!,
        Enum.Parse<Rol>(principal.FindFirstValue(ClaimTypes.Role)!));

    public static string TokenSesion(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(SesionAuthenticationHandler.ClaimToken)!;
}
