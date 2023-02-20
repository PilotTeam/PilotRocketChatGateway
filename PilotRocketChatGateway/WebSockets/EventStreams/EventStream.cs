namespace PilotRocketChatGateway.WebSockets.EventStreams
{
    public interface IEventStream
    {
        void RegisterEvent(dynamic request);
        bool DeleteEvent(dynamic request);
    }
    public abstract class EventStream : IEventStream
    {
        protected Dictionary<string, string> _events { get; } = new Dictionary<string, string>();
        public void RegisterEvent(dynamic request)
        {
            _events[request.@params[0]] = request.id;
        }

        public bool DeleteEvent(dynamic request)
        {
            var e = _events.FirstOrDefault(x => x.Value == request.id).Key;
            if (e == null)
                return false;
            return _events.Remove(e);
        }
    }
}
