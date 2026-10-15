using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Services;
using Framework.Extensions;

namespace Framework.Remoting
{
    public class SponsorshipManager : MarshalByRefObject, ISponsor, ITrackingHandler
    {
        List<ILease> m_LeaseList;
        List<MarshalByRefObject> m_ObjectsToRegister;

        public SponsorshipManager()
        {
            m_LeaseList = new List<ILease>();
            m_ObjectsToRegister = new List<MarshalByRefObject>();
            TrackingServices.RegisterTrackingHandler(this);
        }

        ~SponsorshipManager()
        {
            UnregisterAll();
        }

        public void OnExit(object sender, EventArgs e)
        {
            UnregisterAll();
        }

        public virtual TimeSpan Renewal(ILease lease)
        {
            Debug.Assert(lease.CurrentState == LeaseState.Active);
            return lease.InitialLeaseTime;
        }

        public void Register(MarshalByRefObject obj)
        {
            lock (m_ObjectsToRegister)
            {
                m_ObjectsToRegister.Add(obj);
            }
        }

        private void ProcessRegister(MarshalByRefObject obj)
        {
            ILease lease = GetLease(obj);

            Debug.Assert(lease != null);
            Debug.Assert(lease.CurrentState == LeaseState.Active);

            lease.Register(this);
            lock (m_LeaseList)
            {
                m_LeaseList.Add(lease);
            }
        }

        public static ILease GetLease(MarshalByRefObject obj)
        {
            ILease lease = (ILease) RemotingServices.GetLifetimeService(obj);
            return lease;
        }

        public void Unregister(MarshalByRefObject obj)
        {
            ILease lease = (ILease) RemotingServices.GetLifetimeService(obj);
            if (lease == null)
                return;
            Debug.Assert(lease.CurrentState == LeaseState.Active);

            lease.Unregister(this);
            lock (m_LeaseList)
            {
                m_LeaseList.Remove(lease);
            }
        }

        public void UnregisterAll()
        {
            lock (m_LeaseList)
            {
                while (m_LeaseList.Count > 0)
                {
                    ILease lease = m_LeaseList[m_LeaseList.Count - 1];
                    lease.Unregister(this);
                    m_LeaseList.RemoveAt(m_LeaseList.Count - 1);
                }
            }
        }

        #region ITrackingHandler Members

        public void MarshaledObject(object obj, ObjRef or)
        {
            MarshalByRefObject mbr = obj as MarshalByRefObject;
            if (mbr == null)
                return;
            lock (m_ObjectsToRegister)
            {
                int index = m_ObjectsToRegister.IndexOf(mbr);
                if (index < 0)
                    return;
                ProcessRegister(mbr);
                m_ObjectsToRegister.RemoveAt(index);
            }
        }

        public void UnmarshaledObject(object obj, ObjRef or)
        {

        }

        public void DisconnectedObject(object obj)
        {

        }

        #endregion
    }

    class SessionSponsorshipManager : MarshalByRefObject, ISponsor, ITrackingHandler
    {
        Dictionary<ILease, string> m_LeaseList;
        Dictionary<MarshalByRefObject, string> m_objectsToRegister;
        ISessionProvider m_sessProvider;

        public SessionSponsorshipManager(ISessionProvider sessPrv)
        {
            m_LeaseList = new Dictionary<ILease, string>();
            m_objectsToRegister = new Dictionary<MarshalByRefObject, string>();
            m_sessProvider = sessPrv;
            TrackingServices.RegisterTrackingHandler(this);
        }

        ~SessionSponsorshipManager()
        {
            UnregisterAll();
        }

        public void OnExit(object sender, EventArgs e)
        {
            UnregisterAll();
        }

        public TimeSpan Renewal(ILease lease)
        {
            if (lease == null)
                return TimeSpan.Zero;

            Debug.Assert(lease.CurrentState == LeaseState.Active);

            string key;
            if (m_LeaseList.TryGetValue(lease, out key))
            {
                lock (m_LeaseList)
                {
                    if (m_sessProvider.IsSessionPresent(key))
                        return lease.InitialLeaseTime;
                    else
                    {
                        m_LeaseList.Remove(lease);
                        return TimeSpan.Zero;
                    }
                }
            }
            else
                return TimeSpan.Zero;
        }

        public void Register(MarshalByRefObject obj, string sessionKey)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            lock (m_objectsToRegister)
            {
                m_objectsToRegister[obj] = sessionKey;
            }
        }

        public void Unregister(MarshalByRefObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            ILease lease = (ILease)RemotingServices.GetLifetimeService(obj);
            if (lease == null)
                return;

            Debug.Assert(lease.CurrentState == LeaseState.Active);

            lease.Unregister(this);
            lock (m_LeaseList)
            {
                m_LeaseList.Remove(lease);
            }
        }

        public void UnregisterAll()
        {
            lock (this)
            {
                foreach(ILease lease in  m_LeaseList.Keys)
                {
                    try
                    {
                        lease.Unregister(this);
                    }
                    catch
                    {
                    }
                }

                m_LeaseList.Clear();
            }
        }

        #region ITrackingHandler Members

        public void MarshaledObject(object obj, ObjRef or)
        {
            MarshalByRefObject mbr = obj as MarshalByRefObject;
            if (obj == null)
                return;
            if (m_objectsToRegister.ContainsKey(mbr))
            {
                lock (m_objectsToRegister)
                {
                    if (m_objectsToRegister.ContainsKey(mbr))
                    {
                        string sessionKey = m_objectsToRegister[mbr];
                        ProcessRegister(mbr, sessionKey);
                        m_objectsToRegister.Remove(mbr);
                    }
                }
            }
        }

        void ProcessRegister(MarshalByRefObject obj, string sessionKey)
        {
            ILease lease = SponsorshipManager.GetLease(obj);

            Debug.Assert(lease != null);
            Debug.Assert(lease.CurrentState == LeaseState.Active);

            lease.Register(this);
            lock (m_LeaseList)
            {
                m_LeaseList[lease] = sessionKey;
            }
        }

        public void UnmarshaledObject(object obj, ObjRef or)
        {
        }

        public void DisconnectedObject(object obj)
        {
        }

        #endregion
    }


}
