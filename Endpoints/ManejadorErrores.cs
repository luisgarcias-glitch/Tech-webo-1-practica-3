using CatalogoGalactico.Models;

namespace CatalogoGalactico.Endpoints;

/// <summary>Traduce excepciones de negocio a respuestas HTTP (ProblemDetails): 404, 400.</summary>
public class ManejadorErrores(RequestDelegate next, ILogger<ManejadorErrores> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (NotFoundException ex)
        {
            await Responder(ctx, StatusCodes.Status404NotFound, "Recurso no encontrado", ex.Message);
        }
        catch (ReglaNegocioException ex)
        {
            await Responder(ctx, StatusCodes.Status400BadRequest, "Datos o regla de negocio inválidos", ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            await Responder(ctx, ex.StatusCode, "Solicitud mal formada", ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado");
            await Responder(ctx, StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.");
        }
    }

    private static Task Responder(HttpContext ctx, int status, string titulo, string detalle) =>
        Results.Problem(detail: detalle, title: titulo, statusCode: status).ExecuteAsync(ctx);
}
