namespace Ksema.ObjectModel
{
    public interface IUniqueKeyProvider<TKey>
    {
        TKey UniqueKey { get; }
    }
}