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
        private object _locker2 = new object();

        ConcurrentQueue<Action<string>> _queue = new ConcurrentQueue<Action<string>>();

        public CloudsAuthorizeQueue(IWorkspace workspace, ICloudConnector connector, ILogger<CloudsAuthorizeQueue> logger)
        {
            _workspace = workspace;
            _logger = logger;
            _connector = connector;
            Task.Run(Processing);
        }
        private async void Processing()
        {
            while (true)
            {
                lock (_locker2)
                {
                    Monitor.Wait(_locker2);
                }

                try
                {
                    var cloudToken = await _connector.AutorizeAsync(_workspace, _logger);
                    if (cloudToken != null)
                    {
                        while (_queue.TryDequeue(out var action))
                            action(cloudToken);
                    }
                }
                catch(Exception ex)
                {
                    _logger.Log(LogLevel.Information, ex.Message);
                }
                finally
                {
                    _queue.Clear();
                    Authorizing = false;
                }
            }
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

            lock (_locker)
            {
                if (Authorizing)
                    return;

                Authorizing = true;
            }

            lock (_locker2)
            {
                Monitor.Pulse(_locker2);
            }
        }
    }
}
