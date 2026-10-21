using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.Auth;

public class SesionTests
{
    [Fact]
    [Trait("AC", "AC-12")]
    public async Task Sin_sesion_un_endpoint_protegido_responde_401()
    {
        await using var app = new ApiFactory();
        var cliente = app.CrearCliente();

        var respuesta = await cliente.GetAsync("/api/auth/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-003")]
    public async Task Login_valido_devuelve_200_y_cookie_segura()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        await app.CrearUsuarioAsync(email);
        var cliente = app.CrearCliente();

        var respuesta = await cliente.LoginAsync(email, Clientes.PasswordPorDefecto);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("riesgos_sesion=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var yo = await cliente.GetFromJsonAsync<UsuarioDto>("/api/auth/yo");
        Assert.Equal(email, yo!.Email);
        Assert.Equal("Empleado", yo.Rol);
    }

    [Fact]
    [Trait("FR", "FR-003")]
    public async Task Login_fallido_responde_igual_para_email_inexistente_y_contrasena_incorrecta()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        await app.CrearUsuarioAsync(email);
        var cliente = app.CrearCliente();

        var inexistente = await cliente.LoginAsync("nadie@riesgos.test", Clientes.PasswordPorDefecto);
        var incorrecta = await cliente.LoginAsync(email, "OtraClave999");

        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, incorrecta.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await incorrecta.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("FR", "FR-005")]
    public async Task Cada_uso_renueva_la_ultima_actividad()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        var antes = app.EnScope(db => db.Sesiones.Single().UltimaActividad);

        app.Reloj.Advance(TimeSpan.FromMinutes(5));
        (await cliente.GetAsync("/api/auth/yo")).EnsureSuccessStatusCode();

        var despues = app.EnScope(db => db.Sesiones.Single().UltimaActividad);
        Assert.Equal(antes + TimeSpan.FromMinutes(5), despues);
    }

    [Fact]
    [Trait("AC", "AC-74")]
    [Trait("FR", "FR-032")]
    public async Task Fuera_de_desarrollo_HTTP_redirige_a_HTTPS_y_se_envia_HSTS()
    {
        await using var app = new ApiFactory { Entorno = "Production" };
        var http = app.CreateClient(new() { BaseAddress = new Uri("http://riesgos.test"), AllowAutoRedirect = false });
        var https = app.CreateClient(new() { BaseAddress = new Uri("https://riesgos.test"), AllowAutoRedirect = false });

        var redirigida = await http.GetAsync("/api/auth/yo");
        var segura = await https.GetAsync("/api/auth/yo");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, redirigida.StatusCode);
        Assert.Equal("https", redirigida.Headers.Location!.Scheme);
        Assert.True(segura.Headers.Contains("Strict-Transport-Security"));
    }

    private sealed record UsuarioDto(int Id, string Email, string Rol);
}
