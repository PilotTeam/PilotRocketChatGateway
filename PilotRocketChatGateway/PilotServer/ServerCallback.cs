using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using Ascon.Pilot.Transport;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IChangeNotifier
    {
        void Subscribe(IChangesetListener listener);
        void Subscribe(IPersonChangeListener listener);
    }
    public class ServerCallback : IServerCallback, IChangeNotifier
    {
        private readonly List<WeakReference> _changeListeners = new List<WeakReference>();
        private readonly List<WeakReference> _personListeners = new List<WeakReference>();
        private object _changeLock = new object();
        private object _personLock = new object();

        public void Subscribe(IChangesetListener listener)
        {
            lock (_changeLock)
            {
                _changeListeners.Add(new WeakReference(listener));
            }
        }
        public void Subscribe(IPersonChangeListener listener)
        {
            lock (_personLock)
            {
                _personListeners.Add(new WeakReference(listener));
            }
        }
        public void NotifyChangeAsyncCompleted(DChangeset changeset)
        {
            lock (_changeLock)
            {
                foreach (var l in _changeListeners.ToArray())
                {
                    var listener = l.Target as IChangesetListener;
                    if (listener != null)
                        listener.Notify(changeset.Identity);
                    else
                        _changeListeners.Remove(l);
                }
            }
        }

        public void NotifyChangeAsyncError(Guid identity, ProtoExceptionInfo exception)
        {
        }

        public void NotifyChangeset(DChangeset changeset)
        {
        }

        public void NotifyCommandResult(Guid requestId, byte[] data, ServerCommandResult result)
        {
        }

        public void NotifyCustomNotification(string name, byte[] data)
        {
        }

        public void NotifyDMetadataChangeset(DMetadataChangeset changeset)
        {
        }

        public void NotifyDNotificationChangeset(DNotificationChangeset changeset)
        {
        }

        public void NotifyGeometrySearchResult(DGeometrySearchResult searchResult)
        {
        }

        public void NotifyOrganisationUnitChangeset(OrganisationUnitChangeset changeset)
        {
        }

        public void NotifyPersonChangeset(PersonChangeset changeset)
        {
            lock (_personLock)
            {
                foreach (var l in _changeListeners.ToArray())
                {
                    var listener = l.Target as IPersonChangeListener;
                    if (listener != null)
                        listener.Notify(changeset.Changed);
                    else
                        _changeListeners.Remove(l);
                }
            }
        }

        public void NotifySearchResult(DSearchResult searchResult)
        {
        }
    }
}
