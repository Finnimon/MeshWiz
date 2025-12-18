using System.Diagnostics.CodeAnalysis;

namespace MeshWiz.UpToDate;

public interface IUpToDate
{
    void OutOfDate();
    /// <returns>Whether this is UpToDate</returns>
    bool ConsumeOutOfDate();
}