using Riesgos.Core.Dominio;

namespace Riesgos.Api.Dtos;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record UsuarioResponse(int Id, string Email, Rol Rol);
