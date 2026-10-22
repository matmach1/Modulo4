using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Servicios;

namespace Riesgos.Api.Infraestructura;

// Administrador inicial (FR-006, research R12). En v1 existe un único administrador.
public static class SeedAdministrador
{
    public static async Task EjecutarAsync(IServiceProvider servicios, CancellationToken ct = default)
    {
        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiesgosDbContext>();
        if (await db.Usuarios.AnyAsync(u => u.Rol == Rol.Administrador, ct))
        {
            // Si ya existe, las variables de entorno no lo modifican.
            return;
        }

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = Usuario.NormalizarEmail(config["ADMIN_EMAIL"]);
        var password = config["ADMIN_PASSWORD"];
        if (email.Length == 0 || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "No hay administrador en la base y faltan ADMIN_EMAIL o ADMIN_PASSWORD en el entorno. Definilas para crear el administrador inicial.");
        }

        HashContrasena.ValidarNueva(password, "ADMIN_PASSWORD");
        var hash = scope.ServiceProvider.GetRequiredService<HashContrasena>();
        var tiempo = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        db.Usuarios.Add(new Usuario
        {
            Email = email,
            PasswordHash = hash.Hash(password),
            Rol = Rol.Administrador,
            FechaCreacion = tiempo.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);
    }
}
