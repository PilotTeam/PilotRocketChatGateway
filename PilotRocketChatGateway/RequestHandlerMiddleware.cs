using Microsoft.AspNetCore.Mvc;

namespace PilotRocketChatGateway
{
    public class RequestHandlerMiddleware : Controller
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestHandlerMiddleware(ILogger<RequestHandlerMiddleware> logger, RequestDelegate next)
        {
            _next = next;
            _logger = logger;
        }

        public Task Invoke(HttpContext context)
        {
            _logger.Log(LogLevel.Information, $"User id: {GetUserId(context)} Http method: {context.Request.Method} path: {context.Request.Path} query: {context.Request.QueryString}");
            return _next(context);
        }

        private string GetUserId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("x-user-id", out var header))
                return header;
            else
                return "empty";
        }
    }
}
