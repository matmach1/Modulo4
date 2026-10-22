using System.Net;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US6;

public class LogoutTests
{
    [Fact]
    [Trait("AC", "AC-72")]
    [Trait("FR", "FR-005a")]
    public async Task Tras_cerrar_sesion_la_misma_sesion_responde_401()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var logout = await cliente.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/reportes/mios")).StatusCode);
        Assert.Equal(0, app.EnScope(db => db.Sesiones.Count()));
    }

    [Fact]
    [Trait("FR", "FR-005a")]
    public async Task La_sesion_queda_invalidada_en_el_servidor_aunque_se_reenvie_el_token()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        await app.CrearUsuarioAsync(email);
        var cliente = app.CrearCliente();
        var login = await cliente.LoginAsync(email, Clientes.PasswordPorDefecto);
        var cookie = login.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        await cliente.PostAsync("/api/auth/logout", null);

        // Un cliente nuevo que reenvía el token viejo no es aceptado.
        var otro = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var pedido = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        pedido.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await otro.SendAsync(pedido)).StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-005a")]
    public async Task Cerrar_sesion_no_afecta_otras_sesiones_del_usuario()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        var una = await app.ClienteAutenticadoAsync(email);
        var otra = app.CrearCliente();
        (await otra.LoginAsync(email, Clientes.PasswordPorDefecto)).EnsureSuccessStatusCode();

        await una.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, (await otra.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-12")]
    public async Task Sin_sesion_responde_401()
    {
        await using var app = new ApiFactory();

        var respuesta = await app.CrearCliente().PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
