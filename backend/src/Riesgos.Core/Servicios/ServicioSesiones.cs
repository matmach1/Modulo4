using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Persistencia;

namespace Riesgos.Core.Servicios;

// Sesiones guardadas en el servidor (research R4): permiten cerrar sesión de verdad.
public sealed class ServicioSesiones(RiesgosDbContext db, TimeProvider tiempo)
{
    // Para no escribir en la base en cada request, la actividad se renueva como máximo una vez por minuto.
    private static readonly TimeSpan IntervaloRenovacion = TimeSpan.FromMinutes(1);

    public async Task<string> CrearAsync(int usuarioId, CancellationToken ct = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var ahora = tiempo.GetUtcNow();
        db.Sesiones.Add(new Sesion
        {
            UsuarioId = usuarioId,
            TokenHash = HashToken(token),
            FechaCreacion = ahora,
            UltimaActividad = ahora,
        });
        await db.SaveChangesAsync(ct);
        return token;
    }

    // Devuelve la sesión vigente (con su usuario) o null; una sesión vencida se elimina.
    public async Task<Sesion?> ValidarAsync(string token, CancellationToken ct = default)
    {
        var hash = HashToken(token);
        var sesion = await db.Sesiones.Include(s => s.Usuario).SingleOrDefaultAsync(s => s.TokenHash == hash, ct);
        if (sesion is null)
        {
            return null;
        }

        var ahora = tiempo.GetUtcNow();
        if (!sesion.EstaVigente(ahora))
        {
            db.Sesiones.Remove(sesion);
            await db.SaveChangesAsync(ct);
            return null;
        }

        if (ahora - sesion.UltimaActividad >= IntervaloRenovacion)
        {
            sesion.UltimaActividad = ahora;
            await db.SaveChangesAsync(ct);
        }

        return sesion;
    }

    public async Task CerrarAsync(string token, CancellationToken ct = default)
    {
        var hash = HashToken(token);
        await db.Sesiones.Where(s => s.TokenHash == hash).ExecuteDeleteAsync(ct);
    }

    public async Task CerrarOtrasAsync(int usuarioId, string tokenActual, CancellationToken ct = default)
    {
        var hashActual = HashToken(tokenActual);
        await db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.TokenHash != hashActual).ExecuteDeleteAsync(ct);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
