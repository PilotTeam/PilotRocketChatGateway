using Ascon.Pilot.Common;

namespace PilotRocketChatGateway.PilotServer
{
    public class Credentials
    {
        public string Username { get; private set; }
        public string ProtectedPassword { get; private set; }

        public static Credentials GetConnectionCredentials(string username, string password)
        {
            var credentials = new Credentials
            {
                Username = username,
                ProtectedPassword = password.EncryptAes(),
            };

            return credentials;
        }
    }
}
