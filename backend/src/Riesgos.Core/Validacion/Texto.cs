namespace Riesgos.Core.Validacion;

public static class Texto
{
    public static string Normalizar(string? valor) => (valor ?? string.Empty).Trim();

    // Recorta y valida: un texto solo con espacios cuenta como vacío (research R8).
    public static string Validar(string? valor, string campo, int maximo)
    {
        var normalizado = Normalizar(valor);
        if (normalizado.Length == 0)
        {
            throw new ErrorValidacion(campo, $"El campo '{campo}' es obligatorio.");
        }

        if (normalizado.Length > maximo)
        {
            throw new ErrorValidacion(campo, $"El campo '{campo}' admite como máximo {maximo} caracteres.");
        }

        return normalizado;
    }
}
