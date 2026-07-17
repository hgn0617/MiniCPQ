using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCPQ.Application.Common;

namespace MiniCPQ.Web.Infrastructure;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "请求数据无效"),
            NotFoundException => (StatusCodes.Status404NotFound, "资源不存在"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "没有操作权限"),
            ConflictException or DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "数据冲突"),
            _ => (StatusCodes.Status500InternalServerError, "服务器内部错误")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "未处理的服务器异常");
        }
        else
        {
            logger.LogWarning(exception, "API 请求失败：{Title}", title);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "服务器处理请求时发生错误。"
                    : exception.Message,
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }
}
