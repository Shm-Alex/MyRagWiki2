using System.Collections.Generic;

namespace Framework.Extensions
{
    public interface IInitializable
    {
        void Init(string initData);
    }

    public interface IExtensionModule
    {
        void OnLoad(IDictionary<string, string> arguments);
        void OnUnload();
    }

    public interface ISessionProvider
    {
        bool IsSessionPresent(string sessionId);
        string GetCurrentSessionID();
    }

    interface IRuntimeModuleContext
    {
        ISessionProvider SessionProvider { get;}
        object this[string key] { get;set;}
    }
}
