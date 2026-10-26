using Microsoft.AspNetCore.Diagnostics;
using project.utils.dto;
namespace project.utils.services;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error no controlado al procesar {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new errorMessageDto("No se pudo completar la operación. Inténtalo nuevamente."), cancellationToken);
        return true;
    }
}
