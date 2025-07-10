using Avalonia.Controls;
using Avalonia.Metadata;
using MeshWiz.Abstraction.OpenTK;

namespace MeshWiz.Abstraction.Avalonia;

public sealed class GLWrapper<TWrapped> : Control, IOpenGLControl
where TWrapped : IOpenGLControl
{
    [Content]
    public required TWrapped Wrapped { get; init; }
    public void Dispose() => Wrapped.Dispose();
    public void Init() => Wrapped.Init();
    public void Update(float aspectRatio) => Wrapped.Update(aspectRatio);
    public void Render() => Wrapped.Render();
    public bool GLInitialized=> Wrapped.GLInitialized;
    
}