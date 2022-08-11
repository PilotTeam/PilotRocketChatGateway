using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PilotRocketChatGateway;
using PilotRocketChatGateway.PilotServer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<PilotSettings>(builder.Configuration.GetSection("PilotServer"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddSingleton<IConnectionService, ConnectionService>();
builder.Services.AddSingleton<IRemoteServiceFactory, RemoteServiceFactory>();
builder.Services.AddSingleton<IContextService, ContextService>();

var authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authSettings.Issuer,
        ValidateAudience = false,
        ValidAudience = authSettings.GetAudience(),
        ValidateLifetime = true,
        IssuerSigningKey = authSettings.GetSymmetricSecurityKey(),
        ValidateIssuerSigningKey = true,
        ClockSkew = authSettings.GetClockCrew()
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Request.Headers.TryGetValue("x-auth-token", out var token);
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

app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.UseMiddleware<RequestHandlerMiddleware>();

app.MapControllers();

app.Run();
