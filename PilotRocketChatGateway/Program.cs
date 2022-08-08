using PilotRocketChatGateway;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<PilotSettings>(builder.Configuration.GetSection("PilotServer"));



var app = builder.Build();


app.UseRouting();

app.UseAuthorization();
app.UseWebSockets();

app.UseMiddleware<RequestHandlerMiddleware>();

app.MapControllers();

app.Run();
