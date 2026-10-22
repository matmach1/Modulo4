using Microsoft.AspNetCore.Mvc;
using Riesgos.Api.Auth;
using Riesgos.Api.Dtos;
using Riesgos.Core.Imagenes;
using Riesgos.Core.Servicios;
using Riesgos.Core.Validacion;

namespace Riesgos.Api.Endpoints;

public static class ImagenesEndpoints
{
    // Margen para el resto del formulario multipart por encima de los 2 MB de la imagen.
    private const long LimiteSolicitud = ValidadorImagen.TamanoMaximo + (512 * 1024);

    public static RouteGroupBuilder MapImagenesEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/reportes/{id:int}/imagenes").RequireAuthorization();

        grupo.MapPost("/", async (int id, ServicioImagenes imagenes, HttpContext http, CancellationToken ct) =>
        {
            // Se lee el formulario a mano para exigir exactamente un archivo por solicitud (FR-013).
            if (!http.Request.HasFormContentType)
            {
                throw new ErrorValidacion("archivo", "Enviá la imagen como multipart/form-data en el campo 'archivo'.");
            }

            var formulario = await http.Request.ReadFormAsync(ct);
            if (formulario.Files.Count != 1 || formulario.Files[0].Name != "archivo")
            {
                throw new ErrorValidacion("archivo", "Adjuntá una sola imagen por vez en el campo 'archivo'.");
            }

            var archivo = formulario.Files[0];
            if (archivo.Length > ValidadorImagen.TamanoMaximo)
            {
                throw new ErrorValidacion("archivo", "La imagen supera el máximo de 2 MB.");
            }

            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria, ct);
            var imagen = await imagenes.AdjuntarAsync(id, http.User.UsuarioActual(), archivo.FileName, memoria.ToArray(), ct);
            return Results.Created(ReporteDetalleMapper.UrlImagen(imagen), ReporteDetalleMapper.Imagen(imagen));
        }).WithMetadata(new RequestSizeLimitAttribute(LimiteSolicitud));

        grupo.MapGet("/{imagenId:int}", async (int id, int imagenId, ServicioImagenes imagenes, HttpContext http, CancellationToken ct) =>
        {
            var archivo = await imagenes.ObtenerAsync(id, imagenId, http.User.UsuarioActual(), ct);
            return Results.File(archivo.Ruta, archivo.TipoContenido);
        });

        return grupo;
    }
}
