
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OsmVisualizer.Data
{

    public class Spline
    {
        public List<Vector2> Points;
        

        public Spline(List<Vector2> points)
        {
            Points = points;
        }
        
        public Spline(IEnumerable<Vector2> points)
        {
            Points = points.ToList();
        }

        public Vector2[] EvenlySpacedPoints()
        {
            return Points.ToArray();
        }

        public float GetPointsLength()
        {
            var length = 0f;
            var last = Points[0];
            for (var i = 1; i < Points.Count; i++)
                length += Vector2.Distance(last, last = Points[i]);

            return length;
        }
    }

}