
using System.Collections.Generic;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Math
{
    public static class Math
    {
        // public const int EarthRadius = 6_378_137; //no seams with globe example
        public const int EarthRadius = 6_367_449;
        public const float OriginShift = 2 * Mathf.PI * EarthRadius / 2;
    
        public const float _originShift_div_180 = OriginShift / 180;
    
        public const float _pi_div_2 = Mathf.PI / 2;
        public const float _pi_div_360 = Mathf.PI / 360;
        public const float _pi_div_180 = Mathf.PI / 180;
        public const float _180_div_PI = 180 / Mathf.PI;
        
        
        public const float inch_in_m =    0.0254f;
        public const float feet_in_m =    0.3048f;
        public const float mile_in_m = 1609.34f;

        
        // https://en.wikipedia.org/wiki/Geographic_coordinate_system
        // (Length of a degree)
        public static float WGS84toUTMNorthing (float lat) => lat * OneDegLatInMeters( lat );
        
        public static float WGS84toUTMEasting (float lat, float lon) => lon * OneDegLonInMeters( lat );

        public static float OneDegLatInMeters(float lat)
        {
            var latD = (double) Mathf.Deg2Rad * lat;
            return (float) (
                111_132.92d
                  - 559.82d * System.Math.Cos(2d * latD)
                   + 1.175d * System.Math.Cos(4d * latD)
                  - 0.0023d * System.Math.Cos(6d * latD)
            );
        }

        public static float OneDegLonInMeters(float lat)
        {
            var latD = (double) Mathf.Deg2Rad * lat;
            return (float) (
                111_412.84d * System.Math.Cos(latD)
                    - 93.5d * System.Math.Cos(3d * latD)
                   + 0.118d * System.Math.Cos(5d * latD)
                - 0.000165d * System.Math.Cos(7d * latD)
            );
        }


        
        // @todo https://cdn.jsdelivr.net/npm/geodesy@2/utm.js
        /**
         * Converts a Universal Transverse Mercator coordinate (UTM)
         * to W
         */
        public static float UTMtoWGS84Lat(float x) => x * 90f / 10_000_000f;
        public static float UTMtoWGS84Lon(float x, float y) =>  y * 90f / 10_000_000f;

        public static Vector2 DirectionForPoint(int index, List<Vector2> points, int startOffset = 0, int endOffset = 0)
        {
            var dir = Vector2.zero;
            var curr = points[index];
            if (index > startOffset)
            {
                var prev = points[index - 1];
                dir += curr - prev;
            }

            if (index < points.Count - endOffset - 1)
            {
                var next = points[index + 1];
                dir += next - curr;
            }
                
            dir.Normalize();
            return dir;
        }
        
        public static Vector2 DirectionForPoint(this Vector2[] points, int index, int startOffset = 0, int endOffset = 0)
        {
            var dir = Vector2.zero;
            var curr = points[index];
            if (index > startOffset)
            {
                var prev = points[index - 1];
                dir += curr - prev;
            }

            if (index < points.Length - endOffset - 1)
            {
                var next = points[index + 1];
                dir += next - curr;
            }
                
            dir.Normalize();
            return dir;
        }
        
        public static Vector2 DirectionForPoint(this List<Vector2> points, int index, int startOffset = 0, int endOffset = 0)
        {
            var dir = Vector2.zero;
            var curr = points[index];
            if (index > startOffset)
            {
                var prev = points[index - 1];
                dir += curr - prev;
            }

            if (index < points.Count - endOffset - 1)
            {
                var next = points[index + 1];
                dir += next - curr;
            }
                
            dir.Normalize();
            return dir;
        }
        
        public static bool LinesIntersect(out Vector3 intersection, Vector3[] line1, Vector3[] line2)
        {
            intersection = Vector3.zero;
		
            for (var i = 0; i < line1.Length - 1; i++)
            {
                var a1 = line1[i];
                var a2 = line1[i + 1];
                var va = a2 - a1;
                        
                for (var l = 0; l < line2.Length - 1; l++)
                {
                    var b1 = line2[l];
                    var b2 = line2[l + 1];
                    var vb = b2 - b1;

                    if (!LineLineIntersection(out var intersectionPoint, a1, va, b1, vb)) 
                        continue;
				
                    // check if intersectionPoint is between a1 and a2
                    //   and if intersectionPoint is between b1 and b2
                    if (!PointIsBetweenPoints(a1, a2, intersectionPoint) || !PointIsBetweenPoints(b1, b2, intersectionPoint))
                        continue;
				
                    intersection = intersectionPoint;
                    return true;
                }

            }

            return false;
        }
        
        //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
        //Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
        //same plane, use ClosestPointsOnTwoLines() instead.
        public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){
 
            var lineVec3 = linePoint2 - linePoint1;
            var crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            var crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);
 
            var planarFactor = Vector3.Dot(lineVec3, crossVec1and2);
 
            //is coplanar, and not parallel
            if(Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }
            
            intersection = Vector3.zero;
            return false;
        }
        
        public static bool PointIsBetweenPoints(Vector3 a, Vector3 b, Vector3 point)
        {
            // (distance(A, C) + distance(B, C) == distance(A, B))
            return Mathf.Abs(Vector3.Distance(a, point) + Vector3.Distance(b, point) - Vector3.Distance(a, b)) < 0.001f;
        }
        
        public static bool PointIsInPolygon(Vector2 p, Vector2[] polyPoints)
        {
            var j = polyPoints.Length - 1;
            var inside = false;
            for (int i = 0; i < polyPoints.Length; j = i++)
            {
                var pi = polyPoints[i];
                var pj = polyPoints[j];
                if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        // https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line#Another_vector_formulation
        // https://forum.unity.com/threads/closest-point-on-a-line.121567/
        public static float DistancePointLine(this Vector3 p, Vector3 origin, Vector3 dir)
        {
            // return Vector3.Cross(origin - p, dir).magnitude / dir.magnitude;
            var point2origin = origin - p;
            return ( point2origin - Vector3.Dot(point2origin,dir) * dir ).magnitude;
        }
        
        public static float DistancePointLine(this Vector2 p, Vector2 origin, Vector2 dir)
        {
            return DistancePointLine(p.ToVector3xz(), origin.ToVector3xz(), dir.ToVector3xz());
        }
        
        public static bool IsOutOfBounds(this Vector2 point, Vector2 min, Vector2 max)
        {
            return point.x < min.x || point.x > max.x || point.y < min.y || point.y > max.y;
        }

        public static float DistanceToBounds(this Vector2 p, Vector2 min, Vector2 max)
        {
            var p3 = p.ToVector3xz();
            return Mathf.Min(
                p3.DistancePointLine(min.ToVector3xz(), Vector3.right), 
                p3.DistancePointLine(max.ToVector3xz(), Vector3.right),
                p3.DistancePointLine(min.ToVector3xz(), Vector3.forward),
                p3.DistancePointLine(max.ToVector3xz(), Vector3.forward)
            );
        }
        
        public static int Clamp(int val, int min, int max)
        {
            return System.Math.Max(min, System.Math.Min(max, val));
        }

    }
}