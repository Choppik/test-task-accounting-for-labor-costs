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
                _logger.LogError(ex, "Unhandled exception occurred on path {Path}", httpContext.Request.Path);
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // 1. Ошибки валидации (FluentValidation)
            if (exception is ValidationException ve)
            {
                var problem = new
                {
                    type = "VALIDATION_ERROR",
                    title = "Ошибка валидации",
                    status = (int)HttpStatusCode.BadRequest,
                    message = ve.Message,
                    errors = ve.Errors, // можно отдать список ошибок, если фронтенд умеет
                    traceId = context.TraceIdentifier
                };

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return SendJsonAsync(context, problem);
            }

            // 2. Бизнес-ошибки: InvalidOperationException и любые другие, которые ты хочешь показывать пользователю
            // Сюда попадает твоя ошибка «нет действующей ставки»
            if (exception is InvalidOperationException ioe)
            {
                var problem = new
                {
                    type = "BUSINESS_RULE_VIOLATION",
                    title = "Нарушение бизнес-правила",
                    status = (int)HttpStatusCode.BadRequest,
                    message = ioe.Message,
                    traceId = context.TraceIdentifier
                };

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return SendJsonAsync(context, problem);
            }

            // 3. Ошибки БД (MongoDB, сеть и т.п.)
            if (exception is MongoConnectionException ||
                exception is TimeoutException ||
                exception.InnerException is SocketException)
            {
                var problem = new
                {
                    type = "DATABASE_UNAVAILABLE",
                    title = "Не удалось подключиться к базе данных",
                    status = (int)HttpStatusCode.ServiceUnavailable,
                    message = "Сервис временно недоступен из-за проблем с хранилищем данных.",
                    traceId = context.TraceIdentifier
                };

                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                return SendJsonAsync(context, problem);
            }

            // 4. Всё остальное — реальные сбои
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