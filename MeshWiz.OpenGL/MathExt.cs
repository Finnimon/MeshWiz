using System.Numerics;
using MeshWiz.Math;
using OpenTK.Mathematics;
using Vector4 = OpenTK.Mathematics.Vector4;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace MeshWiz.OpenTK;

public static class MathExt
{
    public static Vector2 ToOpenTK<TNum>(this Vec2<TNum> vec)
        where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum> =>
        new(float.CreateTruncating(vec.X),
            float.CreateTruncating(vec.Y));

    public static Vector3 ToOpenTK<TNum>(this Vec3<TNum> vec)
        where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum> =>
        new(float.CreateTruncating(vec.X),
            float.CreateTruncating(vec.Y),
            float.CreateTruncating(vec.Z));

    public static Vector4 ToOpenTK<TNum>(this Vec4<TNum> vec)
        where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum> =>
        new(float.CreateTruncating(vec.X),
            float.CreateTruncating(vec.Y),
            float.CreateTruncating(vec.Z),
            float.CreateTruncating(vec.W));

    public static Vec4<float> ToVec4(this Color4 color) => new(color.R, color.G, color.B, color.A);

    public static Color4 ToColor4<TNum>(this Vec4<TNum> vec) where TNum : unmanaged, IFloatingPointIeee754<TNum> =>
        new(
            Clamped(vec.X),
            Clamped(vec.Y),
            Clamped(vec.Z),
            Clamped(vec.W)
        );

    private static float Clamped<TNum>(TNum num, float min=0f, float max=1f)
        where TNum : INumber<TNum>
        => float.Clamp(float.CreateTruncating(num),min,max);
}