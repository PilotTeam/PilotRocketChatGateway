using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace PilotRocketChatGateway.Pushes
{
    public interface ICloudsAuthorizeQueue
    {
        void Authorize(Action<string> push);
    }
    public class CloudsAuthorizeQueue : ICloudsAuthorizeQueue
    {
        private readonly IWorkspace _workspace;
        private readonly ILogger _logger;
        private readonly ICloudConnector _connector;
        private bool _authorizing;
        private object _locker = new object();

        ConcurrentQueue<Action<string>> _queue = new ConcurrentQueue<Action<string>>();

        public CloudsAuthorizeQueue(IWorkspace workspace, ICloudConnector connector, ILogger<CloudsAuthorizeQueue> logger)
        {
            _workspace = workspace;
            _logger = logger;
            _connector = connector;
        }
        private bool Authorizing
        {
            get
            {
                lock (_locker)
                {
                    return _authorizing;
                }
            }
            set
            {
                lock (_locker)
                {
                    _authorizing = value;
                }
            }
        }

        public async void Authorize(Action<string> push)
        {
            _queue.Enqueue(push);
            if (Authorizing)
                return;

            Authorizing = true;

            var cloudToken = await _connector.AutorizeAsync(_workspace, _logger);
            if (cloudToken != null) 
            {
                while (_queue.TryDequeue(out var item))
                    item(cloudToken);
            }
            Authorizing = false; 
        }
    }
}
