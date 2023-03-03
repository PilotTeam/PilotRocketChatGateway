using Microsoft.AspNetCore.Authentication.JwtBearer;
using PilotRocketChatGateway.Authentication;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.Utils;
using Serilog;

namespace PilotRocketChatGateway
{
    public static class WebApplicationBuilderExtentions
    {
        public static async Task RegisterInCloudAsync(this WebApplicationBuilder builder, IWorkspace workspace)
        {
            if (workspace.Data != null)
                return;

            var cloudSettings = builder.Configuration.GetSection("RocketChatCloud").Get<RocketChatCloudSettings>();
            if (string.IsNullOrEmpty(cloudSettings.RegistrationToken) == false)
            {
                var cloudConnector = new CloudConnector(new HttpRequestHelper());
                var result = await cloudConnector.RegisterAsync(cloudSettings, Log.Logger);
                if (result != null)
                    workspace.SaveData(result);
            }
        }

        public static void AddAuthentication(this WebApplicationBuilder builder)
        {
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
        }
    }
}
