using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace OsmVisualizer.Math
{
    /**
     * http://wiki.unity3d.com/index.php/Triangulator
     */
    public static class PolygonTriangulation
    {

        public static int[] Triangulate(this IReadOnlyList<Vector3> points)
        {
            var triangulator = new Triangulator(points);

            return triangulator.Triangulate();
        }

        public static int[] Triangulate(this IReadOnlyList<Vector2> points)
        {
            var triangulator = new Triangulator(points);
            
            return triangulator.Triangulate();
            //
            //
            // if (points.Length < 3)
            //     return null;
            //
            // var usedPoints = new bool[points.Length];
            // var triangles = new List<int>();
            //
            // // http://csharphelper.com/blog/2014/07/triangulate-a-polygon-in-c/#:~:text=To%20triangulate%20a%20polygon%2C%20first,of%20the%20polygon's%20other%20points.
            // // Do While (# vertices > 3)
            // //     Find an ear.
            // //     Add the ear to the triangle list.
            // //     Remove the ear's middle vertex from the polygon.
            // //     Loop
            // // Make a triangle from the 3 remaining vertices.
            //
            // var start = 0;
            // for (var count = points.Length; count > 3; count--)
            // {
            //     var earMiddleVertex = FindEar(start, usedPoints, points, triangles);
            //     
            //     if (earMiddleVertex < 0)
            //         return null;
            //     
            //     usedPoints[earMiddleVertex] = true;
            //     start = earMiddleVertex + 1;
            // }
            //
            // var lastTri = new List<int>();
            // for (var i = 0; i < usedPoints.Length; i++)
            // {
            //     if (!usedPoints[i])
            //     {
            //         lastTri.Add(i);
            //     }
            // }
            //
            // if (lastTri.Count == 3)
            // {
            //     triangles.AddRange(lastTri);
            // }
            //
            // return triangles.ToArray();
        }
        
        private static int FindEar(int start, bool[] usedPoints, Vector2[] points, List<int> triangles)
        {
            // @todo works not correctly
            // maybe divide the points array
            
            var isClockwise = false;
            var a = 0;
            var b = 1;
            var c = 2;

            var length = usedPoints.Length;
            
            for (a = start % length; a < (start - 1 + length) % length; a = (a + 1) % length)
            {
                if (usedPoints[a]) continue;
                
                for (b = (a + 1) % length; b < (a - 1 + length % length); b = (b + 1) % length)
                {
                    if (usedPoints[b]) continue;
                    
                    for (c = (b + 1) % length; c < (b - 1 + length) % length; c = (c + 1) % length)
                    {
                        if (usedPoints[c]) continue;
                        
                        isClockwise = new [] {points[a],points[b],points[c]}.IsOrientationClockwise();
                        if (isClockwise) break;
                    }

                    // c %= length;
                    // if (usedPoints[c]) continue;
                    
                    // isClockwise = new [] {points[a],points[b],points[c]}.IsOrientationClockwise();
                    if (isClockwise) break;
                }
                
                // b %= length;
                // if (usedPoints[b]) continue;

                // isClockwise = new [] {points[a],points[b],points[c]}.IsOrientationClockwise();
                if (isClockwise) break;
            }
            
            // a %= length;
            isClockwise = new [] {points[a],points[b],points[c]}.IsOrientationClockwise();
            //
            // // checked all possible vertices
            if (!isClockwise || usedPoints[a])
                return -1;
            
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            return b;
        }

        public static bool IsOrientationClockwise(this Vector2[] points)
        {
            return points.Orientation() > 0f;
        }

        /**
         * If orientation is positive, then a->b->c is clockwise
         * https://www.jofre.de/?p=1440
         */
        public static float Orientation(this Vector2[] points)
        {
            if (points.Length == 0)
                return 0f;
            
            float agg = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                agg += edge(points[i], points[i + 1]);
            }
            return agg + edge(points[points.Length - 1], points[0]);
            //
            // agg > 0 => counterclockwise
            // return crossProductZ(a, b) +
            //        crossProductZ(b, c) +
            //        crossProductZ(c, a);
        }
        /**
         * If orientation is positive, then a->b->c is counterclockwise
         * https://www.jofre.de/?p=1440
         */
        public static float OrientationCrossP(this Vector2[] points)
        {
            float agg = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                agg += crossProductZ(points[i], points[i + 1]);
            }
            return agg + crossProductZ(points[points.Length - 1], points[0]);
            //
            // agg > 0 => counterclockwise
            // return crossProductZ(a, b) +
            //        crossProductZ(b, c) +
            //        crossProductZ(c, a);
        }
        
        public static float crossProductZ(Vector2 a, Vector2 b) {
            return a.x * b.y - a.y * b.x;
        }
        
        // https://stackoverflow.com/a/1165943/6034142
        public static float edge(Vector2 a, Vector2 b) {
            // (x2 − x1)(y2 + y1)
            return (b.x - a.x) * (b.y + a.y);
        }


    }
        
        
}