namespace Riesgos.Core.Validacion;

// Dato de entrada inválido: la API lo traduce a HTTP 400.
public sealed class ErrorValidacion(string campo, string mensaje) : Exception(mensaje)
{
    public string Campo { get; } = campo;
}
