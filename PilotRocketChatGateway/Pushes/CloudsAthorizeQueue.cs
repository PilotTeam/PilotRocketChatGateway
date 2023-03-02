using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace PilotRocketChatGateway.Pushes
{
    public class CloudsAthorizeQueue
    {
        private readonly IWorkspace _workspace;
        private readonly ILogger _logger;

        private bool _authorizing;
        private object _locker = new object();

        ConcurrentQueue<Action<string>> _queue = new ConcurrentQueue<Action<string>>();

        public CloudsAthorizeQueue(IWorkspace workspace, ILogger logger)
        {
            _workspace = workspace;
            _logger = logger;
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

            var cloudToken = await CloudConnector.AutorizeAsync(_workspace, _logger);
            if (cloudToken != null) 
            {
                while (_queue.TryDequeue(out var item))
                    item(cloudToken);
            }
            Authorizing = false; 
        }
    }
}
