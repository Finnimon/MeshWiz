using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MeshWiz.CompilerServices.CodeGen;

[Generator]
public class InlineRefArraySourceGen : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var hasRefStruct = context.CompilationProvider.Select(HasRefStructTest);
        var filter= context.SyntaxProvider.ForAttributeWithMetadataName(
                typeof(InlineRefArrayAttribute).AssemblyQualifiedName!,
                static (_,_) => true,
                static (context, _) => context)
            .Combine(hasRefStruct)
            .Select(static(x,token)=>new object());
    }

    private static bool HasRefStructTest(Compilation compilation, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return compilation.SyntaxTrees.FirstOrDefault()?.Options is CSharpParseOptions
        {
            LanguageVersion: > LanguageVersion.CSharp12
        };
    }




    private static void Execute(SourceProductionContext context, Compilation compilation) { }
}