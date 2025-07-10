// using System.Collections;
// using System.Numerics;
//
// namespace MeshWiz.Math;
//
// [Obsolete]
// internal record ArbitraryVector<TNum>(TNum[] Components) : IVector<ArbitraryVector<TNum>,TNum>
// where TNum:unmanaged, IFloatingPointIeee754<TNum>
// {
//     public IEnumerator<TNum> GetEnumerator()
//     => ((IEnumerable<TNum>)Components).GetEnumerator();
//
//     IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//
//     public TNum this[int index] => Components[index];
//
//     public int CompareTo(ArbitraryVector<TNum>? other)
//         => Length.CompareTo(other?.Length??TNum.Zero);
//     public ReadOnlySpan<TNum> AsSpan() => Components.AsSpan();
//
//     public static ArbitraryVector<TNum> FromComponents(TNum[] components) => new(components);
//
//     public static ArbitraryVector<TNum> FromComponents(ReadOnlySpan<TNum> components)
//         => new([..components]);
//     public static ArbitraryVector<TNum> Zero => new([]);
//     public static ArbitraryVector<TNum> One => new([TNum.One]);
//     public int Count => Components.Length;
//     public static uint Dimensions => 0;
//     private TNum? _length;
//     public TNum Length =>_length ??= TNum.Sqrt(SquaredLength);
//     private TNum? _squaredLength;
//     public TNum SquaredLength =>_squaredLength??=Dot(this);
//
//     public ArbitraryVector<TNum> Add(in ArbitraryVector<TNum> other)
//     {
//         if(Count != other.Count) throw new InvalidOperationException();
//         var components = new TNum[Count];
//         for (int i = 0; i < Count; i++) components[i] = this[i]+other[i];
//         return FromComponents(components);
//     }
//     public ArbitraryVector<TNum> Scale(in TNum scalar)
//     {
//         var components = new TNum[Count];
//         for (var i = 0; i < Count; i++) components[i] = this[i]*scalar;
//         return FromComponents(components);
//     }
//     public TNum Dot(in ArbitraryVector<TNum> other)
//     {
//         if(Count != other.Count) throw new InvalidOperationException();
//         var dot = TNum.Zero;
//         for (int i = 0; i < Count; i++) dot += this[i]*other[i];
//         return dot;
//     }
//
//     public bool IsParallelTo(in ArbitraryVector<TNum> other)
//     {
//         throw new NotImplementedException();
//     }
//     public bool IsParallelTo(in ArbitraryVector<TNum> other, TNum tolerance)
//     {
//         throw new NotImplementedException();
//     }
// }