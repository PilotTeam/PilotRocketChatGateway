using Microsoft.Extensions.Logging;
using Moq;
using PilotRocketChatGateway;
using PilotRocketChatGateway.Pushes;

namespace PilotRocketChatTests
{
    public class Tests
    {
        private Mock<IWorkspace> _workSpace;
        private Mock<ICloudsAuthorizeQueue> _authorizeQueue;
        private PushGatewayConnector _connector;

        [SetUp]
        public void Setup()
        {
            _workSpace = new Mock<IWorkspace>();
            _workSpace.Setup(x => x.Data).Returns(new WorkspaceData());
            _authorizeQueue = new Mock<ICloudsAuthorizeQueue>();
            _connector = new PushGatewayConnector(_workSpace.Object, _authorizeQueue.Object, new Mock<ILogger<PushGatewayConnector>>().Object);
        }

        [Test]
        public void Test1()
        {
            var token = "token";
            _authorizeQueue.Setup(x => x.Authorize(It.IsAny<Action<string>>())).Callback<Action<string>>((a)=>
            {
                a(token);
            });

            var options = new PushOptions() { sender = new User() };
            _connector.SendPushAsync(new PushToken(), options);
            _connector.SendPushAsync(new PushToken(), options);
        }
    }
}