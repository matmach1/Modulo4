using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Tests.Infraestructura;

public static class Clientes
{
    public const string PasswordPorDefecto = "Clave12345";

    // HTTPS para que el cliente envíe la cookie Secure; sin seguir redirecciones para poder verlas.
    public static HttpClient CrearCliente(this ApiFactory app) =>
        app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    public static string EmailNuevo() => $"empleado-{Guid.NewGuid():N}@riesgos.test";

    // Crea el usuario directamente en la base (sin pasar por el registro) y devuelve su id.
    public static async Task<int> CrearUsuarioAsync(this ApiFactory app, string email, string password = PasswordPorDefecto, Rol rol = Rol.Empleado)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiesgosDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<HashContrasena>();
        var usuario = new Usuario
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = hash.Hash(password),
            Rol = rol,
            FechaCreacion = app.Reloj.GetUtcNow(),
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario.Id;
    }

    public static async Task<HttpResponseMessage> LoginAsync(this HttpClient cliente, string email, string password) =>
        await cliente.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<HttpClient> ClienteAutenticadoAsync(this ApiFactory app, string? email = null, Rol rol = Rol.Empleado)
    {
        email ??= EmailNuevo();
        await app.CrearUsuarioAsync(email, PasswordPorDefecto, rol);
        var cliente = app.CrearCliente();
        var respuesta = await cliente.LoginAsync(email, PasswordPorDefecto);
        respuesta.EnsureSuccessStatusCode();
        return cliente;
    }

    public static async Task<HttpClient> ComoAdminAsync(this ApiFactory app)
    {
        var cliente = app.CrearCliente();
        var respuesta = await cliente.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        respuesta.EnsureSuccessStatusCode();
        return cliente;
    }

    public static T EnScope<T>(this ApiFactory app, Func<RiesgosDbContext, T> consulta)
    {
        using var scope = app.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<RiesgosDbContext>());
    }

    // Procesa los trabajos encolados con el procesador real y el clasificador simulado.
    public static async Task ProcesarClasificacionesAsync(this ApiFactory app)
    {
        app.Cola.Procesar ??= async (trabajo, ct) =>
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ProcesadorClasificacion>().ProcesarAsync(trabajo, ct);
        };
        await app.Cola.ProcesarPendientesAsync();
    }

    // Procesa la cola con un clasificador que no responde: avanza el reloj 10 s cuando ya fue llamado.
    public static async Task ProcesarSinRespuestaAsync(this ApiFactory app)
    {
        app.Clasificador.NoResponder = true;
        var llamadasPrevias = app.Clasificador.Entradas.Count;
        var procesando = app.ProcesarClasificacionesAsync();
        var limite = DateTime.UtcNow.AddSeconds(5);
        while (app.Clasificador.Entradas.Count == llamadasPrevias && !procesando.IsCompleted && DateTime.UtcNow < limite)
        {
            await Task.Delay(10);
        }

        app.Reloj.Advance(TimeSpan.FromSeconds(10));
        await procesando.WaitAsync(TimeSpan.FromSeconds(5));
        app.Clasificador.NoResponder = false;
    }
}
