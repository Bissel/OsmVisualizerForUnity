using System.Collections.Generic;
using System.Globalization;
using OsmVisualizer.Data.Types;
using UnityEngine;

namespace OsmVisualizer.Data.Utils
{
    
    public static class VectorExtensions
    {
        
        
        public static Vector3 ToVector3xz(this Vector2 v, float y = 0f)
        {
            return new Vector3(v.x, y, v.y);
        }

        public static Vector2 ToVector2xz(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }
        
        public static Vector2 PositionToVector(this Position2 pos)
        {
            
            // Lat = (-)Down (+)Up 
            // Lon = (-)Left (+)Right 
            
            // X = (-)Left (+)Right 
            // Z = (-)Back (+)Forward // Y if Vector2
            
            // return new Vector2(
            //     pos.Lon * pos.OneDegLonInMeters(),
            //     pos.Lat * pos.OneDegLatInMeters()
            // );
            
            return new Vector2(
                pos.Lon * Math.Math._originShift_div_180, 
                Mathf.Log(Mathf.Tan( (90 + pos.Lat) * Math.Math._pi_div_360) ) / Math.Math._pi_div_180 * Math.Math._originShift_div_180
            );
        }

        public static Vector2 Lerp2Clamp(Vector2 min, Vector2 max, Vector2 position)
        {
            return new Vector2(
                position.x, // - min.x, // / max.x - min.x,
                position.y // - min.y  // / max.y - min.y
            );
        }

        // @todo https://cdn.jsdelivr.net/npm/geodesy@2/utm.js
        // public static Position2 VectorToPosition(this Vector2 pos)
        // {
        //     return new Position2(
        //         Math.UTMtoWGS84Lat(pos.x),
        //         Math.UTMtoWGS84Lon(pos.x, pos.y)
        //     );
        //     var vx = (pos.x / Math.OriginShift) * 180;
        //     var vy = (pos.y / Math.OriginShift) * 180;
        //     vy = (float) ( Math._180_div_PI * (2 * System.Math.Atan( System.Math.Exp(vy * Math._pi_div_180) ) - Math._pi_div_2) );
        //     return new Position2(vx, vy);
        // }


        public static Vector2 Rotate(this Vector2 pos, float deg)
        {
            var sin = Mathf.Sin(deg * Mathf.Deg2Rad);
            var cos = Mathf.Cos(deg * Mathf.Deg2Rad);
            
            return new Vector2(
                cos * pos.x - sin * pos.y,
                sin * pos.x + cos * pos.y
            );
        }
        
        public static Vector3 RotateAroundY(this Vector3 pos, float deg)
        {
            var sin = Mathf.Sin(deg * Mathf.Deg2Rad);
            var cos = Mathf.Cos(deg * Mathf.Deg2Rad);
            
            return new Vector3(
                cos * pos.x - sin * pos.z,
                pos.y,
                sin * pos.x + cos * pos.z
            );
        }

        public static Vector3 ClipYGreater(this Vector3 v, float clipGreater)
        {
            if (float.IsNaN(clipGreater) || v.y <= clipGreater)
                return v;

            return new Vector3(v.x, clipGreater, v.z);
        }

        public static bool IsInBounds(this Vector2 p, Vector2 boundsMin, Vector2 boundsMax)
        {
            return p.x >= boundsMin.x && p.x <= boundsMax.x
                && p.y >= boundsMin.y && p.y <= boundsMax.y;
        }

        // https://wiki.unity3d.com/index.php/PolyContainsPoint
        public static bool InsidePolygon(this Vector2 p, List<Vector2> polygon)
        {
            var j = polygon.Count - 1;
            var inside = false;
            for (int i = 0; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                    inside = !inside;
            }
            return inside;
        }
        
        public static bool InsidePolygon(this Vector3 p3, List<Vector3> polygon)
        {
            var p = p3.ToVector2xz();
            var j = polygon.Count - 1;
            var inside = false;
            for (int i = 0; i < polygon.Count; j = i++)
            {
                var pi = polygon[i].ToVector2xz();
                var pj = polygon[j].ToVector2xz();
                if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        public static Vector3 Round(this Vector3 v, int precision = 100)
        {
            return new Vector3(
                Mathf.Round(v.x * precision) / precision,
                Mathf.Round(v.y * precision) / precision,
                Mathf.Round(v.z * precision) / precision
            );
        }
        
        public static Vector2 Round(this Vector2 v, int precision = 100)
        {
            return new Vector2(
                Mathf.Round(v.x * precision) / precision,
                Mathf.Round(v.y * precision) / precision
            );
        }
    }
}
