using System;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PilotRocketChatGateway.Authentication
{

    [Serializable]
    public class AuthSettings
    {
        public string Issuer { get; set; } // издатель токена
        public string SecretKey { get; set; } // ключ для шифрации

        public SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        }

        public string GetAudience()
        {
            return "PilotRocketChatGateway"; // потребитель токена
        }
        public TimeSpan GetClockCrew()
        {
            return TimeSpan.Zero;
        }

    }
}
