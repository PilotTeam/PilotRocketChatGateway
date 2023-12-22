using PilotRocketChatGateway.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PilotRocketChatTests
{
    public class UriExtentionsTests
    {
        [Test]
        public void ShouldGetParameterFromInvalidUri()
        {
            string msgId = "de7edc54-8cb3-457b-91e4-25647c4a477b";
            string invalidUri = $"http://0.0.0.0:5053/group/#chatname?msg={msgId}";
            var uri = new Uri(invalidUri);

            StringAssert.AreEqualIgnoringCase(msgId, uri.GetParameter("msg"));
        }
    }
}
