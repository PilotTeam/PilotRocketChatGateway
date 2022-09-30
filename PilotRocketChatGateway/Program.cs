using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PilotRocketChatGateway;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var authHelper = new AuthHelper();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.Configure<PilotSettings>(builder.Configuration.GetSection("PilotServer"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddSingleton<IContextFactory, ContextFactory>();
builder.Services.AddSingleton<IWebSocketsServiceFactory, WebSocketsServiceFactory>();
builder.Services.AddSingleton<IWebSocketSessionFactory, WebSocketSessionFactory>();
builder.Services.AddSingleton<IContextService, ContextService>();
builder.Services.AddSingleton<IAuthHelper, AuthHelper>();

var authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var authHelper = new AuthHelper();
    options.TokenValidationParameters = authHelper.GetTokenValidationParameters(authSettings);
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Request.Headers.TryGetValue(AuthHelper.AUTH_HEADER_NAME, out var token);
            if (string.IsNullOrEmpty(token))
            {
                context.NoResult();
                return Task.CompletedTask;
            }

            context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();


app.UseRouting();
app.Use((context, next) =>
{
    context.Request.EnableBuffering();
    return next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.UseMiddleware<RequestHandlerMiddleware>();

app.MapControllers();

app.Run();
