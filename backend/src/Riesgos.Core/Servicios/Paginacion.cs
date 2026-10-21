using Riesgos.Core.Validacion;

namespace Riesgos.Core.Servicios;

public sealed record Pagina<T>(IReadOnlyList<T> Items, int Page, int Size, int Total);

public static class Paginacion
{
    public const int TamanoPorDefecto = 20;
    public const int TamanoMaximo = 100;

    // Tamaño por defecto 20 y máximo 100; fuera de rango es dato inválido (FR-024, FR-011).
    public static (int Page, int Size) Validar(int? page, int? size)
    {
        var p = page ?? 1;
        var s = size ?? TamanoPorDefecto;
        if (p < 1)
        {
            throw new ErrorValidacion("page", "La página debe ser 1 o mayor.");
        }

        if (s < 1 || s > TamanoMaximo)
        {
            throw new ErrorValidacion("size", $"El tamaño de página debe estar entre 1 y {TamanoMaximo}.");
        }

        return (p, s);
    }
}
