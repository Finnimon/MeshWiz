using System.Numerics;

namespace MeshWiz.Math;

/// <summary>
/// Useful number constants
/// </summary>
/// <typeparam name="TNum">Target number type</typeparam>
public static class Numbers<TNum>
    where TNum : INumberBase<TNum>
{
    public static readonly TNum ThirtyTwo = TNum.CreateTruncating(32);

    /// <summary>
    /// <c>=> 2</c>
    /// </summary>
    public static readonly TNum Two = TNum.CreateTruncating(2);
    /// <summary>
    /// <c>=> 0.5</c>
    /// </summary>
    public static readonly TNum Half = TNum.One/Two;
    /// <summary>
    /// <c>=> 3</c>
    /// </summary>
    public static readonly TNum Three = TNum.CreateTruncating(3);
    /// <summary>
    /// <c>=> 2</c>
    /// </summary>
    public static readonly TNum RootTwo = TNum.CreateTruncating(double.Sqrt(2f));
    
    /// <summary>
    /// <c>=> 4</c>
    /// </summary>
    public static readonly TNum Four = TNum.CreateTruncating(4f);
    /// <summary>
    /// <c>=> 1e-1</c>
    /// </summary>
    public static readonly TNum Eps1 = TNum.CreateTruncating(1e-1);
    /// <summary>
    /// <c>=> 1e-2</c>
    /// </summary>
    public static readonly TNum Eps2 = TNum.CreateTruncating(1e-2);
    /// <summary>
    /// <c>=> 1e-3</c>
    /// </summary>
    public static readonly TNum Eps3 = TNum.CreateTruncating(1e-3);
    /// <summary>
    /// <c>=> 1e-4</c>
    /// </summary>
    public static readonly TNum Eps4 = TNum.CreateTruncating(1e-4);
    /// <summary>
    /// <c>=> 1e-5</c>
    /// </summary>
    public static readonly TNum Eps5 = TNum.CreateTruncating(1e-5);
    /// <summary>
    /// <c>=> 1e-6</c>
    /// </summary>
    public static readonly TNum Eps6 = TNum.CreateTruncating(1e-6);
    /// <summary>
    /// <c>=> 1e-7</c>
    /// </summary>
    public static readonly TNum Eps7 = TNum.CreateTruncating(1e-7);
    /// <summary>
    /// <c>=> 1e-8</c>
    /// </summary>
    public static readonly TNum Eps8 = TNum.CreateTruncating(1e-8);
    /// <summary>
    /// <c>=> 1e-8</c>
    /// </summary>
    public static readonly TNum Eps9 = TNum.CreateTruncating(1e-9);
    /// <summary>
    /// <c>=> 1e-10</c>
    /// </summary>
    public static readonly TNum Eps10 = TNum.CreateTruncating(1e-10);

    public static readonly TNum Fourth =TNum.CreateTruncating(0.25);
    public static readonly TNum TwoPi = TNum.CreateTruncating(2 * double.Pi);
    public static readonly TNum Third = TNum.CreateTruncating(1.0 / 3.0);
}