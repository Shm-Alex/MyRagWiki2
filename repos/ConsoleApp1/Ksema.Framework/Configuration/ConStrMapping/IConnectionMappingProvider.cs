using System;
using System.Collections.Generic;
using System.Text;
using Framework.Collections;
using Microsoft.Win32;

namespace Ksema.Framework.Configuration.ConStrMapping
{
    public interface IConnectionMappingProvider
    {
        ConnectionProfile GetDefaultProfile();
        void SetDefaultProfile(ConnectionProfile profile);
        IList<ConnectionProfile> GetAvailableProfiles();
        void SaveProfile(ConnectionProfile profile);
        void DeleteProfile(string Name);

        string GetConnectionString(string Name, string defValue);
    }

    public class ConnectionProfile
    {
        private string m_name;
        private Dictionary<string, string> connections = new Dictionary<string, string>();

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public string this[string connName]
        {
            get
            {
                if (connections.ContainsKey(connName))
                    return connections[connName];
                else
                    return null;
            }

            set
            {
                connections[connName] = value;
            }
        }

        public override bool Equals(object obj)
        {
            return m_name.Equals(obj);
        }

        public override int GetHashCode()
        {
            return m_name.GetHashCode();
        }
    }

    public class RegistryConnectionMappingProvider : IConnectionMappingProvider
    {
        const string connectionmapping = "ConnectionMapping";
        public ConnectionProfile GetDefaultProfile()
        {

            RegistryKey root = GetRootKey();
            string profileName = root.GetValue("DefaultProfile") as string;
            if (profileName == null)
                return null;
            return GetProfileByName(profileName, root);
        }

        private static ConnectionProfile GetProfileByName(string profileName, RegistryKey root)
        {
            ConnectionProfile profile = new ConnectionProfile();
            profile.Name = profileName;
            RegistryKey prof = root.OpenSubKey(profileName, false);
            foreach (string s in prof.GetSubKeyNames())
            {
                RegistryKey sk = prof.OpenSubKey(s);
                string name = (string) sk.GetValue("name");
                string value = (string)sk.GetValue("value");
                profile[name] = value;
            }
            return profile;
        }

        private RegistryKey GetRootKey()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(connectionmapping);
            if (key == null)
            {
                return Registry.CurrentUser.CreateSubKey(connectionmapping);
            }
            return key;
        }

        public void SetDefaultProfile(ConnectionProfile profile)
        {
            throw new NotImplementedException();
        }

        public IList<ConnectionProfile> GetAvailableProfiles()
        {
            RegistryKey root = GetRootKey();
            List<ConnectionProfile> l_pr = new List<ConnectionProfile>();
            foreach (string s in root.GetSubKeyNames())
            {
                l_pr.Add(GetProfileByName(s,root));
            }
            return l_pr;
        }

        public void SaveProfile(ConnectionProfile profile)
        {
            throw new NotImplementedException();
        }

        public string GetConnectionString(string Name, string DefName)
        {
            throw new NotImplementedException();
        }

        #region IConnectionMappingProvider Members


        public void DeleteProfile(string Name)
        {
            if ( GetDefaultProfile().Name == Name)
                GetRootKey().DeleteValue("DefaultProfile");
            GetRootKey().DeleteSubKey(Name);
        }

        #endregion
    }
}
