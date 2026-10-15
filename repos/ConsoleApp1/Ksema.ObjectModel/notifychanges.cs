using System;

namespace Ksema.ObjectModel
{
    public interface INotifyLinkChanged
    {
        event LinkChangedEventHandler LinkAdded;
        event LinkChangedEventHandler LinkDeleted;
    }

    public interface INotifyValueChanged
    {
        event ValueChangedEventHandler ValueChanged;
    }

    //с этим еще надо решить, насколько оно полезно...
    public interface ITrackChanges
    {
        void SetupValueChanges(INotifyValueChanged eventSource);
        void SetupLinkChanges(INotifyLinkChanged eventSource);
        void SetupSingleLinkChanges(INotifyValueChanged eventSource);
    }

    public delegate void LinkChangedEventHandler(object sender, LinkChangedEventArgs args);
    public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs args);

    public class LinkChangedEventArgs : EventArgs
    {
        public LinkChangedEventArgs(string name, object objA, object objB)
        {
            Name = name;
            ObjectA = objA;
            ObjectB = objB;
        }

        public object ObjectA { get; private set; }
        public object ObjectB { get; private set; }
        public string Name { get; private set; }
    }

    public class ValueChangedEventArgs : EventArgs
    {
        public ValueChangedEventArgs(object owner, string aPropertyName, object aOld, object aNew, bool forSave)
        {
            Owner = owner;
            PropertyName = aPropertyName;
            OldValue = aOld;
            NewValue = aNew;
            ForSave = forSave;
        }

        public object Owner { get; private set; }
        public string PropertyName { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }
        public bool ForSave { get; private set; }
    }

}
