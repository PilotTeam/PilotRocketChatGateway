using Ascon.Pilot.Common;

namespace PilotRocketChatGateway.UserContext
{
    public class UserData
    {
        public string Username { get; private set; }
        public string ProtectedPassword { get; private set; }

        public static UserData GetConnectionCredentials(string username, string password)
        {
            var credentials = new UserData
            {
                Username = username,
                ProtectedPassword = password.EncryptAes(),
            };

            return credentials;
        }
    }
}
