using Riesgos.Core.Dominio;

namespace Riesgos.Core.Servicios;

// Quién hace la operación; los servicios lo usan para verificar permisos (Principio III).
public sealed record UsuarioActual(int Id, string Email, Rol Rol)
{
    public bool EsAdministrador => Rol == Rol.Administrador;
}
