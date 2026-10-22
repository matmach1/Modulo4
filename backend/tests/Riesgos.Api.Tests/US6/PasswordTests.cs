using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US6;

public class PasswordTests
{
    private static Task<HttpResponseMessage> CambiarAsync(HttpClient cliente, string? actual, string? nueva) =>
        cliente.PutAsJsonAsync("/api/auth/password", new { passwordActual = actual, passwordNueva = nueva });

    [Fact]
    [Trait("AC", "AC-14")]
    public async Task Con_la_actual_correcta_solo_la_nueva_permite_iniciar_sesion()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        var cliente = await app.ClienteAutenticadoAsync(email);

        var respuesta = await CambiarAsync(cliente, Clientes.PasswordPorDefecto, "NuevaClave2026");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CrearCliente().LoginAsync(email, Clientes.PasswordPorDefecto)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.CrearCliente().LoginAsync(email, "NuevaClave2026")).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-32")]
    public async Task Con_la_actual_incorrecta_responde_400_y_sigue_valiendo_la_anterior()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        var cliente = await app.ClienteAutenticadoAsync(email);

        var respuesta = await CambiarAsync(cliente, "NoEsLaActual", "NuevaClave2026");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.CrearCliente().LoginAsync(email, Clientes.PasswordPorDefecto)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CrearCliente().LoginAsync(email, "NuevaClave2026")).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-66")]
    public async Task Una_nueva_de_menos_de_8_caracteres_responde_400_y_sigue_valiendo_la_anterior()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        var cliente = await app.ClienteAutenticadoAsync(email);

        var respuesta = await CambiarAsync(cliente, Clientes.PasswordPorDefecto, "corta");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.CrearCliente().LoginAsync(email, Clientes.PasswordPorDefecto)).StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-004")]
    public async Task Cambiar_la_contrasena_cierra_las_demas_sesiones_y_mantiene_la_actual()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        var actual = await app.ClienteAutenticadoAsync(email);
        var otra = app.CrearCliente();
        (await otra.LoginAsync(email, Clientes.PasswordPorDefecto)).EnsureSuccessStatusCode();

        await CambiarAsync(actual, Clientes.PasswordPorDefecto, "NuevaClave2026");

        Assert.Equal(HttpStatusCode.OK, (await actual.GetAsync("/api/auth/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await otra.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-12")]
    public async Task Sin_sesion_responde_401()
    {
        await using var app = new ApiFactory();

        var respuesta = await CambiarAsync(app.CrearCliente(), "x", "NuevaClave2026");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
