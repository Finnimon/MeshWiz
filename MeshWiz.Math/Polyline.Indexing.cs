using System.Numerics;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Indexing
    {
        public static (LineIndexer[] Indices, TVector[] Vertices) Indicate<TVector, TNum>
            (IEnumerable<Line<TVector, TNum>> lines)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVector : unmanaged, IFloatingVector<TVector, TNum>
        {
            if (lines is IReadOnlyList<Line<TVector, TNum>> lineList) return Indicate(lineList);
            List<LineIndexer> indices = [];
            List<TVector> vertices = [];
            Dictionary<TVector, int> unified = [];

            foreach (var line in lines)
            {
                var start = IndexerUtilities.GetIndex(line.Start, unified, vertices);
                var end = IndexerUtilities.GetIndex(line.End, unified, vertices);
                indices.Add(new LineIndexer(start, end));
            }

            return ([..indices], [..vertices]);
        }

        public static (LineIndexer[] Indices, TVector[] Vertices) Indicate<TVector, TNum>
            (IReadOnlyList<Line<TVector, TNum>> lines)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVector : unmanaged, IFloatingVector<TVector, TNum>
        {
            var indices = new LineIndexer[lines.Count];
            var averageUniqueVertices = lines.Count / 2;
            var vertices = new List<TVector>(averageUniqueVertices);
            var unified = new Dictionary<TVector, int>(averageUniqueVertices);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var start =IndexerUtilities.GetIndex(line.Start, unified, vertices);
                var end = IndexerUtilities.GetIndex(line.End, unified, vertices);
                indices[i] = new LineIndexer(start, end);
            }

            return (indices, [..vertices]);
        }
    }
}