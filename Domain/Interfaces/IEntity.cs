namespace Interfaces;
public interface IEntity<TKey>
{
    public TKey Id { get; set; }
}
