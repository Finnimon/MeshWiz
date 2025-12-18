using System.Collections;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Collections;

/// <seealso href="https://github.com/dotnet/runtime/blob/6e1e6b1f34ac821c47364f5b0baf91d18e1fcbe7/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L1185"/>
public struct Enumerator<TList,T> : IEnumerator<T>, IEnumerator
    where TList:IVersionedList<T>
{
    private readonly TList _list;
    private int _index;
    private readonly int _version;
    private T? _current;

    public Enumerator(TList list)
    {
        _list = list;
        _index = 0;
        _version = list.Version;
        _current = default;
    }

    public void Dispose() { }

    [SuppressMessage("ReSharper", "InvertIf",Justification = "Intended order of operations")]
    public bool MoveNext()
    {
        if (_version == _list.Version && ((uint)_index < (uint)_list.Count))
        {
            _current = _list[_index];
            _index++;
            return true;
        }

        return MoveNextRare();
    }

    private bool MoveNextRare()
    {
        if (_version != _list.Version)
            ThrowHelper.ThrowInvalidOperationException();

        _index = _list.Count + 1;
        _current = default;
        return false;
    }

    public T Current => _current!;

    object? IEnumerator.Current
    {
        get
        {
            if (_index == 0 || _index == _list.Count + 1) ThrowHelper.ThrowInvalidOperationException();
            return Current;
        }
    }

    void IEnumerator.Reset()
    {
        if (_version != _list.Version) ThrowHelper.ThrowInvalidOperationException();

        _index = 0;
        _current = default;
    }
}