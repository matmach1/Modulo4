using System.Text;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Servicios;

// BCrypt (RNF-05). El factor de trabajo es configurable para que los tests no sean lentos.
public sealed class HashContrasena(int factorTrabajo = 12)
{
    public const int LargoMinimo = 8;

    // BCrypt ignora lo que pase de 72 bytes: se rechaza en lugar de truncar en silencio.
    public const int BytesMaximos = 72;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, factorTrabajo);

    // Un hash con formato inválido (por ejemplo, una cuenta deshabilitada) nunca valida.
    public bool Verificar(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception e) when (e is BCrypt.Net.SaltParseException or ArgumentException)
        {
            return false;
        }
    }

    public static void ValidarNueva(string? password, string campo = "password")
    {
        if (password is null || password.Length < LargoMinimo)
        {
            throw new ErrorValidacion(campo, $"La contraseña debe tener al menos {LargoMinimo} caracteres.");
        }

        if (Encoding.UTF8.GetByteCount(password) > BytesMaximos)
        {
            throw new ErrorValidacion(campo, "La contraseña es demasiado larga.");
        }
    }
}
