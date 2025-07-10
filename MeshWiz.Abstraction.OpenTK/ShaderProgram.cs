using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public sealed record ShaderProgram(int Handle)
{
    private bool _disposed = false;
    public void Bind() => GL.UseProgram(Handle);
    
    public ShaderProgram BindAnd()
    {
        Bind();
        return this;
    }
    public void Unbind() => GL.UseProgram(0);
    public void Delete()=>GL.DeleteProgram(Handle);
    public void Dispose()
    {
        if (_disposed) return; 
        _disposed = true;
        Unbind();
        Delete();
    }
    public int GetUniformLocation(string name) => GL.GetUniformLocation(Handle, name);
    public ShaderProgram SetUniform(string name,in Vector4 vec)
    {
        var loc = GetUniformLocation(name);
        GL.Uniform4(loc, vec);
        return this;
    }

    public ShaderProgram SetUniform(string name,in Vector3 vec)
    {
        var loc = GetUniformLocation(name);
        GL.Uniform3(loc, vec);
        return this;
    }
    
    public ShaderProgram SetUniform(string name,in Vector2 vec)
    {
        var loc = GetUniformLocation(name);
        GL.Uniform2(loc, vec);
        return this;
    }

    public ShaderProgram SetUniform(string name, float value)
    {
        var loc = GetUniformLocation(name);
        GL.Uniform1(loc, value);
        return this;
    }

    public ShaderProgram SetUniform(string name,ref Matrix4 matrix)
    {
        var loc=GetUniformLocation(name);
        GL.UniformMatrix4(loc,false,ref matrix);
        return this;
    }

    public ShaderProgram SetUniform(string name, ref Matrix3 matrix)
    {
        var loc = GetUniformLocation(name);
        GL.UniformMatrix3(loc, false, ref matrix);
        return this;
    }
    public ShaderProgram SetUniform(string name,in Color4 color)
    {
        var loc = GetUniformLocation(name);
        GL.Uniform4(loc, color);
        return this;
    }

    private const int IllegalHandle = -1;
    public static ShaderProgram Create(string vertexShader, string fragmentShader, string? geometryShader = null)
    {
        var handle= GL.CreateProgram();
        var vert=CompileAttach(handle,vertexShader,ShaderType.VertexShader);
        var frag=CompileAttach(handle,fragmentShader,ShaderType.FragmentShader);
        var geo=geometryShader is not null? CompileAttach(handle,geometryShader,ShaderType.GeometryShader): IllegalHandle;
        GL.LinkProgram(handle);
        DetachDelete(handle,vert);
        DetachDelete(handle,frag);
        if(geo is not IllegalHandle) DetachDelete(handle,geo);
        var shader=new ShaderProgram(handle);
        return shader;
    }

    public static ShaderProgram FromFiles(string basePath)
    {
        var dir= Path.GetDirectoryName(basePath);
        var fragFile=Path.ChangeExtension(basePath, ".frag");
        var fragCode = File.ReadAllText(fragFile);
        
        var vertFile=Path.ChangeExtension(basePath, ".vert");
        var vertCode=File.ReadAllText(vertFile);
        
        var geoFile=Path.ChangeExtension(basePath, ".geo");
        var geoCode=File.Exists(geoFile) ? File.ReadAllText(geoFile) : null;
        
        return Create(vertCode, fragCode, geoCode);
    }

    public static int CompileAttach(int programHandle, string shaderCode, ShaderType shaderType)
    {
        var shader = GL.CreateShader(shaderType);
        GL.ShaderSource(shader, shaderCode);
        GL.CompileShader(shader);
        GL.AttachShader(programHandle, shader);
        LogShaderInfo(shader, shaderType);
        return shader;
    }

    public static void LogShaderInfo(int shaderHandle,ShaderType type)
    {
        var info=GL.GetShaderInfoLog(shaderHandle);
        if(info is not {Length:>0}) return;
        Console.Error.WriteLine($"{type} info: {info}");
    }

    public static void DetachDelete(int programHandle, int shaderHandle)
    {
        GL.DetachShader(programHandle, shaderHandle);
        GL.DeleteShader(shaderHandle);
    }
    public int GetAttribLoc(string name) => GL.GetAttribLocation(Handle, name);
    
}