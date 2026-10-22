using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Riesgos.Core;
using Riesgos.Core.Validacion;

namespace Riesgos.Api.Infraestructura;

// Traduce las excepciones de negocio a ProblemDetails (RFC 9457) con el código de contracts/api.md.
public sealed class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excepcion, CancellationToken ct)
    {
        var (estado, titulo) = excepcion switch
        {
            ErrorValidacion e => (StatusCodes.Status400BadRequest, e.Message),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "La solicitud tiene un formato inválido."),
            AccesoDenegado e => (StatusCodes.Status403Forbidden, e.Message),
            NoEncontrado e => (StatusCodes.Status404NotFound, e.Message),
            Conflicto e => (StatusCodes.Status409Conflict, e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "El reporte fue modificado por otra operación. Volvé a cargarlo."),
            _ => (0, string.Empty),
        };

        if (estado == 0)
        {
            return false;
        }

        if (estado == StatusCodes.Status403Forbidden)
        {
            // Registro de accesos denegados sin datos sensibles (research R13).
            logger.LogWarning("Acceso denegado a {Metodo} {Ruta}", http.Request.Method, http.Request.Path);
        }

        http.Response.StatusCode = estado;
        var detalle = new ProblemDetails { Status = estado, Title = titulo };
        if (excepcion is ErrorValidacion validacion)
        {
            detalle.Extensions["campo"] = validacion.Campo;
        }

        return await problemas.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = detalle, Exception = excepcion });
    }
}
