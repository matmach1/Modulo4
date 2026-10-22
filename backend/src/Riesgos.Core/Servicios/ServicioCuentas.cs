using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Servicios;

public sealed partial class ServicioCuentas(RiesgosDbContext db, HashContrasena hash, TimeProvider tiempo, ServicioSesiones sesiones)
{
    // Restricción UNIQUE violada en SQLite (código de error extendido 2067).
    private const int SqliteUniqueViolado = 2067;

    // Hash de una contraseña cualquiera para verificar aunque el email no exista: así un login
    // fallido tarda lo mismo en ambos casos y no revela qué emails tienen cuenta por tiempo.
    private readonly Lazy<string> _hashFicticio = new(() => hash.Hash("contraseña-ficticia"));

    // Devuelve el usuario si las credenciales son válidas; null en cualquier otro caso.
    public async Task<Usuario?> AutenticarAsync(string? email, string? password, CancellationToken ct = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == normalizado, ct);
        var valida = hash.Verificar(password ?? string.Empty, usuario?.PasswordHash ?? _hashFicticio.Value);
        return usuario is not null && valida ? usuario : null;
    }

    // Registro con rol siempre Empleado (FR-001, FR-002).
    public async Task<Usuario> RegistrarAsync(string? email, string? password, CancellationToken ct = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        if (normalizado.Length > 254 || !FormatoEmail().IsMatch(normalizado))
        {
            throw new ErrorValidacion("email", "El email no tiene un formato válido.");
        }

        HashContrasena.ValidarNueva(password);

        if (await db.Usuarios.AnyAsync(u => u.Email == normalizado, ct))
        {
            throw new Conflicto("Ya existe una cuenta con ese email.");
        }

        var usuario = new Usuario
        {
            Email = normalizado,
            PasswordHash = hash.Hash(password!),
            Rol = Rol.Empleado,
            FechaCreacion = tiempo.GetUtcNow(),
        };
        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolado })
        {
            // Dos registros simultáneos con el mismo email: el índice único deja pasar uno solo (AC-63).
            throw new Conflicto("Ya existe una cuenta con ese email.");
        }

        return usuario;
    }

    // Verifica la actual (FR-004). Si cambia, cierra las demás sesiones del usuario y mantiene la
    // actual (research R4).
    public async Task CambiarPasswordAsync(UsuarioActual usuario, string tokenSesionActual, string? actual, string? nueva, CancellationToken ct = default)
    {
        var cuenta = await db.Usuarios.SingleAsync(u => u.Id == usuario.Id, ct);
        if (!hash.Verificar(actual ?? string.Empty, cuenta.PasswordHash))
        {
            throw new ErrorValidacion("passwordActual", "La contraseña actual no es correcta.");
        }

        HashContrasena.ValidarNueva(nueva, "passwordNueva");
        cuenta.PasswordHash = hash.Hash(nueva!);
        await db.SaveChangesAsync(ct);
        await sesiones.CerrarOtrasAsync(usuario.Id, tokenSesionActual, ct);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoEmail();
}
