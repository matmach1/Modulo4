namespace Riesgos.Core.Dominio;

public class Sesion
{
    public static readonly TimeSpan Inactividad = TimeSpan.FromHours(24);

    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    // SHA-256 del token que viaja en la cookie; el token en claro nunca se guarda.
    public required string TokenHash { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public DateTimeOffset UltimaActividad { get; set; }

    public bool EstaVigente(DateTimeOffset ahora) => UltimaActividad + Inactividad >= ahora;
}
