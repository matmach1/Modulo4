using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.US1;

public class RegistroTests
{
    private static Task<HttpResponseMessage> RegistrarAsync(HttpClient cliente, object cuerpo) =>
        cliente.PostAsJsonAsync("/api/auth/registro", cuerpo);

    [Fact]
    [Trait("AC", "AC-13")]
    public async Task Registro_crea_la_cuenta_y_permite_iniciar_sesion()
    {
        await using var app = new ApiFactory();
        var cliente = app.CrearCliente();
        var email = Clientes.EmailNuevo();

        var respuesta = await RegistrarAsync(cliente, new { email, password = "Clave12345" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var login = await cliente.LoginAsync(email, "Clave12345");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-001")]
    public async Task Registro_asigna_siempre_el_rol_empleado()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();

        var respuesta = await RegistrarAsync(app.CrearCliente(), new { email, password = "Clave12345", rol = "Administrador" });

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.Equal("Empleado", cuerpo!.Rol);
        Assert.Equal(Rol.Empleado, app.EnScope(db => db.Usuarios.Single(u => u.Email == email).Rol));
    }

    [Fact]
    [Trait("AC", "AC-31")]
    public async Task Email_ya_registrado_responde_409_tambien_con_otra_capitalizacion()
    {
        await using var app = new ApiFactory();
        var cliente = app.CrearCliente();
        var email = Clientes.EmailNuevo();
        await RegistrarAsync(cliente, new { email, password = "Clave12345" });

        var mismo = await RegistrarAsync(cliente, new { email, password = "OtraClave99" });
        var mayusculas = await RegistrarAsync(cliente, new { email = "  " + email.ToUpperInvariant(), password = "OtraClave99" });

        Assert.Equal(HttpStatusCode.Conflict, mismo.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, mayusculas.StatusCode);
        Assert.Equal(1, app.EnScope(db => db.Usuarios.Count(u => u.Email == email)));
    }

    [Fact]
    [Trait("AC", "AC-63")]
    public async Task Registros_simultaneos_con_el_mismo_email_crean_una_sola_cuenta()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => RegistrarAsync(app.CrearCliente(), new { email, password = "Clave12345" })));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(1, app.EnScope(db => db.Usuarios.Count(u => u.Email == email)));
    }

    [Theory]
    [Trait("AC", "AC-64")]
    [InlineData("noesmail")]
    [InlineData("")]
    [InlineData("ana@")]
    public async Task Email_invalido_responde_400_y_no_crea_la_cuenta(string email)
    {
        await using var app = new ApiFactory();

        var respuesta = await RegistrarAsync(app.CrearCliente(), new { email, password = "Clave12345" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(1, app.EnScope(db => db.Usuarios.Count())); // solo el administrador inicial
    }

    [Fact]
    [Trait("AC", "AC-65")]
    public async Task Contrasena_de_menos_de_8_caracteres_responde_400_y_no_crea_la_cuenta()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();

        var respuesta = await RegistrarAsync(app.CrearCliente(), new { email, password = "1234567" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, app.EnScope(db => db.Usuarios.Count(u => u.Email == email)));
    }

    private sealed record UsuarioDto(int Id, string Email, string Rol);
}
