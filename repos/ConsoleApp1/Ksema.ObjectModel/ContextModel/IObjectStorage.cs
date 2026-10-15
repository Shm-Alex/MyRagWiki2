using System;

namespace Ksema.ObjectModel.ContextModel
{
    public interface IObjectStorage
    {
        void Save();

        event EventHandler BeforeCreate;
        event EventHandler AfterCreate;
        event EventHandler BeforeUpdate;
        event EventHandler AfterUpdate;
        event EventHandler BeforeDelete;
        event EventHandler AfterDelete;
	}
}