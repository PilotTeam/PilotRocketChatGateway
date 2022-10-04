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
        public int TokenLifeTimeDays { get; set; } // время жизни токена
        public int IdleSessionTimeout { get; set; } // время неактивности пользователя. По окончанию этого таймаута сбрасывается подключение к Pilot

        public SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        }

        public string GetAudience()
        {
            return "PilotRocketChatGateway"; // потребитель токена
        }
        public DateTime? GetTokenLifetime(int days)
        {
            return DateTime.UtcNow.AddDays(days); // время жизни токена
        }
        public TimeSpan GetClockCrew()
        {
            return TimeSpan.Zero;
        }

    }
}
