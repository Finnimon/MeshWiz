using Avalonia.Controls;
using Avalonia.Metadata;
using MeshWiz.Abstraction.OpenTK;

namespace MeshWiz.Abstraction.Avalonia;

public sealed class GLWrapper<TWrapped> : Control, IOpenGLControl
where TWrapped : IOpenGLControl
{
    [Content]
    public required TWrapped Unwrap { get; init; }
    public void Dispose() => Unwrap.Dispose();
    public void Init() => Unwrap.Init();
    public void Update(float aspectRatio) => Unwrap.Update(aspectRatio);
    public void Render() => Unwrap.Render();
    public bool GLInitialized=> Unwrap.GLInitialized;
    
}