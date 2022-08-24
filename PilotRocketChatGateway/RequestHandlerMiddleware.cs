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

        public async Task Invoke(HttpContext context)
        {
            _logger.Log(LogLevel.Information, $"Http method: {context.Request.Method} path: {context.Request.Path} query: {context.Request.QueryString}");
            await _next(context);
        }
    }
}
