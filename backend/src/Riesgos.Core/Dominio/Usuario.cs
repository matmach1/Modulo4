namespace Riesgos.Core.Dominio;

public class Usuario
{
    public int Id { get; set; }

    // Siempre normalizado: sin espacios alrededor y en minúsculas.
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Rol Rol { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public static string NormalizarEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
