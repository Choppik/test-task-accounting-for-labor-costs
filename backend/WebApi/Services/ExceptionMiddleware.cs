using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
                _logger.LogError(ex, "Unhandled exception");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // --- ГЛАВНОЕ ИЗМЕНЕНИЕ: обработка ошибок валидации ---
            if (exception is ValidationException ve)
            {
                var errors = ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(err => err.ErrorMessage).ToArray()
                    );

                var problem = new
                {
                    title = "Validation Error",
                    detail = "Данные не прошли валидацию",
                    status = (int)HttpStatusCode.BadRequest,
                    errors // это тот самый JSON: { "Duration": ["Должна быть больше 0"], "EmployeeId": ["Обязательное поле"] }
                };

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return SendJsonAsync(context, problem);
            }

            // Для всех остальных ошибок — стандартный ответ
            var genericProblem = new
            {
                title = "Internal Server Error",
                detail = context.Request.Path + " : " + exception.Message,
                status = (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return SendJsonAsync(context, genericProblem);
        }

        // Вынес в отдельный метод, чтобы не дублировать сериализацию
        private static Task SendJsonAsync<T>(HttpContext context, T obj)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            return context.Response.WriteAsJsonAsync(obj);
        }
    }
}