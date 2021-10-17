using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OsmVisualizer.Data.Utils
{
    public static class ListExtensions
    {
        // https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize) 
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static string DebugString(this IEnumerable<Vector2> list)
            => list?.Aggregate("", (s, v) => s + $" ({v.x}, {v.y}),")?.OsmSubstring(1, -1);
        
        public static string DebugString(this IEnumerable<Vector3> list)
            => list?.Aggregate("", (s, v) => s + $" ({v.x}, {v.y}, {v.z}),")?.OsmSubstring(1, -1);

        public static bool IsInBounds(this IEnumerable<Vector2> list, Vector2 boundsMin, Vector2 boundsMax)
        {
            return list.Any(p => p.IsInBounds(boundsMin, boundsMax));
        }

        public static void Bounds(this IEnumerable<Vector2> list, out Vector2 boundsMin, out Vector2 boundsMax)
        {
            var boundingMinX = float.MaxValue;
            var boundingMinY = float.MaxValue;
            var boundingMaxX = float.MinValue;
            var boundingMaxY = float.MinValue;

            foreach (var p in list)
            {
                if (p.x < boundingMinX) boundingMinX = p.x;
                if (p.y < boundingMinY) boundingMinY = p.y;
                if (p.x > boundingMaxX) boundingMaxX = p.x;
                if (p.y > boundingMaxY) boundingMaxY = p.y;
            }

            boundsMin = new Vector2(boundingMinX, boundingMinY);
            boundsMax = new Vector2(boundingMaxX, boundingMaxY);
        }

        public static int ClosestIndex(this List<Vector2> list, Vector2 point)
        {
            return list.ClosestIndex(point, out _);
        }
        
        public static int ClosestIndex(this List<Vector2> list, Vector2 point, out float closestDist)
        {
            closestDist = float.MaxValue;
            var index = -1;
            for (var i = 0; i < list.Count; i++)
            {
                var p = list[i];
                var dist = Vector2.Distance(p, point);
                if (dist > closestDist)
                    continue;

                closestDist = dist;
                index = i;
            }

            return index;
        }
        
    }
}