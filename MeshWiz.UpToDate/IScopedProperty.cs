namespace MeshWiz.UpToDate;

public interface IScopedProperty<T>
{
    T Value { get; set; }
    IUpToDate Scope { get; }
}