using Avalonia.Controls;
using Avalonia.Metadata;
using MeshWiz.Abstraction.OpenTK;

namespace MeshWiz.Abstraction.Avalonia;

public sealed class GLWrapper<TWrapped> : Control, IOpenGLControl
where TWrapped : class, IOpenGLControl
{

    public bool Show { get=>Unwrap.Show; set=>Unwrap.Show=value; }

    [Content]
    public required TWrapped Unwrap { get; init; }

    public static implicit operator TWrapped(GLWrapper<TWrapped> wrapper) => wrapper.Unwrap;
    public static implicit operator GLWrapper<TWrapped>(TWrapped data) => new() { Unwrap = data };
    public void Dispose() => Unwrap.Dispose();
    public void Init() => Unwrap.Init();
    public void Update(float aspectRatio) => Unwrap.Update(aspectRatio);
    public void Render() => Unwrap.Render();
    public bool GLInitialized=> Unwrap.GLInitialized;

    /// <inheritdoc />
    public void OutOfDate()
        => Unwrap.OutOfDate();

    /// <inheritdoc />
    public bool ConsumeOutOfDate()
        => Unwrap.ConsumeOutOfDate();
}