using Ascon.Pilot.DataClasses;
using Moq;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PilotRocketChatTests
{
    public class ServerCallbackTests
    {
        [Test]
        public void should_notify_person_listener()
        {
            var serverCallback = new ServerCallback();
            var personListener = new Mock<IPersonChangeListener>();
            serverCallback.Subscribe(personListener.Object);
            var change = new List<DPerson> { new DPerson() { Id = 1 } };
            serverCallback.NotifyPersonChangeset(new PersonChangeset(change));
            personListener.Verify(x => x.Notify(change), Times.Once);
        }
    }
}
