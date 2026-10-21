using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;

namespace Riesgos.Core.Servicios;

public sealed class ServicioCuentas(RiesgosDbContext db, HashContrasena hash)
{
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
}
