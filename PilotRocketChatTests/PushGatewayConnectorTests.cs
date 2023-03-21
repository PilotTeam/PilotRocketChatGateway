using Microsoft.Extensions.Logging;
using Moq;
using PilotRocketChatGateway;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.Utils;
using System.Net;

namespace PilotRocketChatTests
{
    public class Tests
    {
        private Mock<IWorkspace> _workSpace;
        private Mock<ICloudsAuthorizeQueue> _authorizeQueue;
        private Mock<IHttpRequestHelper> _requestHelper;
        private PushGatewayConnector _connector;

        [SetUp]
        public void Setup()
        {
            _workSpace = new Mock<IWorkspace>();
            _workSpace.Setup(x => x.Data).Returns(new WorkspaceData());
            _authorizeQueue = new Mock<ICloudsAuthorizeQueue>();
            _requestHelper = new Mock<IHttpRequestHelper>();
            _connector = new PushGatewayConnector(_workSpace.Object, _authorizeQueue.Object, _requestHelper.Object, new Mock<ILogger<PushGatewayConnector>>().Object);
        }

        [Test]
        public void ShouldAuthorize()
        {
            var token = "token";
            _requestHelper.Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), $"Bearer {token}")).Returns(Task.FromResult(("", HttpStatusCode.OK)));

            _requestHelper.Setup(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(x => x != $"Bearer {token}"))).Returns(Task.FromResult(("", HttpStatusCode.Unauthorized)));

            int authorized = 0;
            _authorizeQueue.Setup(x => x.Authorize(It.IsAny<Action<string>>())).Callback<Action<string>>((a)=>
            {
                authorized++;
                a(token);
            });

            var options = new PushOptions() { sender = new User() };

            //1 push
            _connector.SendPushAsync(new PushToken(), options, string.Empty);

            _requestHelper.Verify(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), $"Bearer {token}"), Times.Once());
            Assert.AreEqual(1, authorized);

            //2 push
            _connector.SendPushAsync(new PushToken(), options, string.Empty);
            _requestHelper.Verify(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), $"Bearer {token}"), Times.Exactly(2));
            Assert.AreEqual(1, authorized);


            //3 push 
            token = "token 2";

            _connector.SendPushAsync(new PushToken(), options, string.Empty);
            _requestHelper.Verify(x => x.PostJsonAsync(It.IsAny<string>(), It.IsAny<string>(), $"Bearer {token}"), Times.Exactly(1)); //push with new token
            Assert.AreEqual(2, authorized);

        }
    }
}