using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IChangesetSender
    {
        Task ChangeAsync(DChangesetData changeset);
    }
    public class ChangesetSender : IChangesetSender
    {
        private readonly IServerApi _serverApi;
        private readonly IChangeNotifier _changeNotifier;

        public ChangesetSender(IServerApi serverApi, IChangeNotifier changeNotifier)
        {
            _serverApi = serverApi;
            _changeNotifier = changeNotifier;
        }
        public Task ChangeAsync(DChangesetData changeset)
        {
            var tcs = new TaskCompletionSource<bool>();
            _serverApi.ChangeAsync(changeset);
            _changeNotifier.Subscribe(new ChangesetSubscription(changeset.Identity, () => tcs.TrySetResult(true)));
            return tcs.Task;
        }

    }

    public interface IChangesetListener
    {
        void Notify(Guid identity);
    }
    public class ChangesetSubscription : IChangesetListener
    {
        private Guid _identity;
        private Action _action;

        public ChangesetSubscription(Guid identity, Action action)
        {
            _identity = identity;
            _action = action;
        }
        public void Notify(Guid identity)
        {
            if (identity == _identity)
                _action();
        }
    }
}
