using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PilotRocketChatGateway;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.UserContext;
using PilotRocketChatGateway.WebSockets;
using Serilog;
using System.Text;
using Serilog.Events;
using PilotRocketChatGateway.Pushes;

var builder = WebApplication.CreateBuilder(args);
var authHelper = new AuthHelper();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.Configure<PilotSettings>(builder.Configuration.GetSection("PilotServer"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddSingleton<IContextFactory, ContextFactory>();
builder.Services.AddSingleton<IWebSocketsServiceFactory, WebSocketsServiceFactory>();
builder.Services.AddSingleton<IWebSocketsWatcher, WebSocketsWatcher>();
builder.Services.AddSingleton<IWebSocketBank, WebSocketBank>();
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

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var cloudSettings = builder.Configuration.GetSection("RocketChatCloud").Get<RocketChatCloudSettings>();
if (string.IsNullOrEmpty(cloudSettings.RegistrationToken) == false)
    Task.Run(() =>
    {
        CloudWorkspace.RegisterAsync(cloudSettings, Log.Logger);
    });

builder.Logging.ClearProviders();
builder.Services.AddLogging(loggingBuilder =>
    loggingBuilder.AddSerilog(dispose: true));

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
