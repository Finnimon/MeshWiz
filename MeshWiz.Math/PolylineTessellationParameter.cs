using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public readonly record struct PolylineTessellationParameter<TNum>(TNum MaxAbsDeviation, TNum MaxAngularDeviation)
where TNum:IFloatingPointIeee754<TNum>
{
    public bool AnglesUnsupported=>MaxAngularDeviation <=TNum.Zero||TNum.IsNaN(MaxAngularDeviation);

    public static readonly PolylineTessellationParameter<TNum> Default = new()
        { MaxAbsDeviation = Numbers<TNum>.Eps2, MaxAngularDeviation = Numbers<TNum>.Eps2 };

    public (int count, TNum countNum, TNum stepSize) GetStepsForAngle(TNum angle)
    {
        if(AnglesUnsupported) throw new InvalidOperationException($"No {nameof(MaxAngularDeviation)} is specified");
        
        var countNum = TNum.Round(angle / MaxAngularDeviation, MidpointRounding.AwayFromZero);
        countNum=TNum.Abs(countNum);
        countNum = TNum.Max(countNum, TNum.One);
        var stepSize = angle / countNum;
        return (int.CreateSaturating(countNum), countNum, stepSize);
    }
}