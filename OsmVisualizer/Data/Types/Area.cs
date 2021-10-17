using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    public class Area : Spline
    {
        private int[] _indices;
        public IEnumerable<int> GetIndices() => _indices ??= this.Triangulate();
        
        public Area(IReadOnlyCollection<Vector3> points) : base(points) {}
        
        public Area(IReadOnlyList<Vector2> points, float height) : base(points, height) {}

        public Area(IReadOnlyList<Vector2> points, IReadOnlyList<float> elevations = null): base(points, elevations) {}


        public override void SetForwardStartAndEnd(Vector2 rotationStart, Vector2 rotationEnd) {}
        public override void SetForwardStartAndEnd(Vector3 rotationStart, Vector3 rotationEnd) {}

        protected override void Init()
        {
            var last = this[0];
            
            var elevationChange = last.y;

            TotalLength = 0f;
            Length.Add(0f);
            
            for (var i = 1; i < Count; i++)
            {
                var p = this[i];
                AddDirAndLength(p, last);
                
                last = p;
                elevationChange += p.y;
            }
            
            AddDirAndLength(this[0], last);

            HasElevation = elevationChange > 0.001f || elevationChange < 0.001f;
        }
    }
}