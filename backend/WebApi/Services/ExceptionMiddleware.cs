using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WebApi.Services
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Логируем ПОЛНУЮ ошибку (со стеком) — это для тебя, в консоль/Serilog
                _logger.LogError(ex, "Unhandled exception occurred on path {Path}", httpContext.Request.Path);

                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // 1. Валидация
            if (exception is ValidationException ve) { /* ... */ }

            // 2. Ошибки БД (MongoDB, SQL) — отдельная категория
            if (exception is MongoConnectionException ||
                exception is TimeoutException ||
                exception.InnerException is SocketException)
            {
                var problem = new
                {
                    type = "DATABASE_UNAVAILABLE",
                    title = "Не удалось подключиться к базе данных",
                    status = (int)HttpStatusCode.ServiceUnavailable, // 503 вместо 500
                    message = "Сервис временно недоступен из-за проблем с хранилищем данных.",
                    traceId = context.TraceIdentifier
                };
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                return SendJsonAsync(context, problem);
            }

            // 3. Всё остальное
            var genericProblem = new
            {
                type = "INTERNAL_ERROR",
                title = "Произошла ошибка на сервере",
                status = (int)HttpStatusCode.InternalServerError,
                message = "Сервис временно недоступен. Попробуйте позже.",
                traceId = context.TraceIdentifier
            };

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return SendJsonAsync(context, genericProblem);
        }

        private static Task SendJsonAsync<T>(HttpContext context, T obj)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            return context.Response.WriteAsJsonAsync(obj);
        }
    }
}