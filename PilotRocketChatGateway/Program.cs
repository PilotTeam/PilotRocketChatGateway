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
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway.Pushes;
using Microsoft.Extensions.DependencyInjection;

AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.Configure<PilotSettings>(builder.Configuration.GetSection("PilotServer"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddSingleton<IContextFactory, ContextFactory>();
builder.Services.AddSingleton<IWebSocketsServiceFactory, WebSocketsServiceFactory>();
builder.Services.AddSingleton<IWebSocketSessionFactory, WebSocketSessionFactory>();
builder.Services.AddSingleton<IContextsBank, ContextsBank>();
builder.Services.AddSingleton<IHttpRequestHelper, HttpRequestHelper>();
builder.Services.AddSingleton<ICloudConnector, CloudConnector>();
builder.Services.AddSingleton<ICloudsAuthorizeQueue, CloudsAuthorizeQueue>();
builder.Services.AddSingleton<IAuthHelper, AuthHelper>();
builder.Services.AddSingleton<IWorkspace, Workspace>();
builder.Services.AddSingleton<IPushGatewayConnector, PushGatewayConnector>();


builder.AddAuthentication();
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

builder.RegisterInCloudAsync(app.Services.GetService<IWorkspace>());


app.Run();