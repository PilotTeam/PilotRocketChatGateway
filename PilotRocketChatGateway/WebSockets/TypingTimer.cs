using Ascon.Pilot.DataClasses;

namespace PilotRocketChatGateway.WebSockets
{
    public class TypingTimer : IDisposable
    {
        
        private readonly System.Timers.Timer _timer;
        private readonly Action<string, int> _startedTyping;
        private readonly Action<string, int> _stoppedTyping;

        public TypingTimer(Action<string, int> startedTyping, Action<string, int> stoppedTyping)
        {
            _timer = new System.Timers.Timer(10000)
            {
                AutoReset = false,
                Enabled = false 
            };
       
            _startedTyping = startedTyping;
            _stoppedTyping = stoppedTyping;
        }

        public void Start(string roomId, int personId)
        {
            if (_timer.Enabled)
                _timer.Stop();

            _startedTyping(roomId, personId);
            _timer.Elapsed += (o, e) => _stoppedTyping(roomId, personId);
            _timer.Start();
        }
        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
