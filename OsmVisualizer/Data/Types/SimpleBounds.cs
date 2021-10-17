using System.Collections.Generic;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    public class SimpleBounds
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;
        
        public SimpleBounds(IEnumerable<Vector2> list)
        {
            list.Bounds(out Min, out Max);
        }

        public bool Intersects(SimpleBounds bounds)
        {
            return bounds.Min.IsInBounds(Min, Max) || bounds.Max.IsInBounds(Min, Max);
        }

        public bool Intersects(Vector2 center, float radius)
        {
            return center.IsInBounds(Min, Max)
                   || Vector2.Distance(center, Min) <= radius
                   || Vector2.Distance(center, Max) <= radius
                   || Vector2.Distance(center, new Vector2(Min.x, Max.y)) <= radius
                   || Vector2.Distance(center, new Vector2(Max.x, Min.y)) <= radius;
        }

        public bool Intersects(IEnumerable<Vector2> list)
        {
            return list.IsInBounds(Min, Max);
        }
    }
}