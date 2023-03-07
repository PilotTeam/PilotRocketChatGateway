using Microsoft.Extensions.Logging;
using Moq;
using PilotRocketChatGateway.Pushes;
using PilotRocketChatGateway.Utils;
using PilotRocketChatGateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PilotRocketChatTests
{
    internal class CloudsAthorizeQueueTests
    {
        private Mock<ICloudConnector> _cloudConnector;
        private CloudsAuthorizeQueue _queue;
        private Mock<IWorkspace> _workSpace;

        [SetUp]
        public void Setup()
        {
            _workSpace = new Mock<IWorkspace>();
            _workSpace.Setup(x => x.Data).Returns(new WorkspaceData());
            _cloudConnector = new Mock<ICloudConnector>();
            _queue = new CloudsAuthorizeQueue(_workSpace.Object, _cloudConnector.Object, new Mock<ILogger<CloudsAuthorizeQueue>>().Object);
        }

        [Test]
        public void ShouldAuthorizeAndPush()
        {
            var token = "token";
            _cloudConnector.Setup(x => x.AutorizeAsync(_workSpace.Object, It.IsAny<ILogger>())).Returns(Task.FromResult(token));

            int pushCalled = 0;
            var push = new Action<string>((s) =>
            {
                pushCalled++;
            });
            _queue.Authorize(push);

            Thread.Sleep(200);

            Assert.AreEqual(1, pushCalled);
            _cloudConnector.Verify(x => x.AutorizeAsync(_workSpace.Object, It.IsAny<ILogger>()), Times.Once);
        }

        [Test]
        public void ShouldAuthorize2()
        {
            var token = "token";
            _cloudConnector.Setup(x => x.AutorizeAsync(_workSpace.Object, It.IsAny<ILogger>())).Returns(Task.Run(() =>
            {
                Thread.Sleep(500);
                return token;
            }));

            int pushCalled = 0;
            var push = new Action<string>((s) =>
            {
                pushCalled++;
            });

            _queue.Authorize(push);
            _queue.Authorize(push);
            _queue.Authorize(push);
            _queue.Authorize(push);

            Thread.Sleep(1000);

            Assert.AreEqual(4, pushCalled);
            _cloudConnector.Verify(x => x.AutorizeAsync(_workSpace.Object, It.IsAny<ILogger>()), Times.Once);
        }
    }
}
