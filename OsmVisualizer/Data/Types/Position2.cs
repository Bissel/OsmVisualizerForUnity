using System;
using System.Globalization;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    [Serializable]
    public class Position2
    {
        // ReSharper disable once InconsistentNaming
        public float Lat;
        // ReSharper disable once InconsistentNaming
        public float Lon;

        private float _oneDegLatInMeters = float.NaN;
        private float _oneDegLonInMeters = float.NaN;
        
        public float OneDegLatInMeters() => float.IsNaN(_oneDegLatInMeters) ? _oneDegLatInMeters = Mathf.Abs(Math.Math.OneDegLatInMeters(Lat)) : _oneDegLatInMeters;
        public float OneDegLonInMeters() => float.IsNaN(_oneDegLonInMeters) ? _oneDegLonInMeters = Mathf.Abs(Math.Math.OneDegLonInMeters(Lat)) : _oneDegLonInMeters;

        public Position2()
        {
            Lat = 0.0f;
            Lon = 0.0f;
        }

        public Position2(float latitude, float longitude)
        {
            Lat = latitude;
            Lon = longitude;
        }
        
        public Position2(float latitude, float longitude, float oneDegLatInMeters, float oneDegLonInMeters)
        {
            Lat = latitude;
            Lon = longitude;

            _oneDegLatInMeters = oneDegLatInMeters;
            _oneDegLonInMeters = oneDegLonInMeters;
        }

        public Position2 WithOffset(Vector2 offsetInMeters, bool exact = false)
        {
            var latInM = exact ? Math.Math.OneDegLatInMeters(Lat) : OneDegLatInMeters();
            var lonInM = exact ? Math.Math.OneDegLonInMeters(Lat) : OneDegLonInMeters();
            
            return new Position2(
                Lat + offsetInMeters.y / latInM,
                Lon + offsetInMeters.x / lonInM,
                latInM,
                lonInM
            );
        }

        // public static Position2 fromVector(Vector2 inMeters)
        // {
        //     return inMeters.VectorToPosition();
        // }

        /**
         * Returns the Projected Coords in meters
         */
        public Vector2 InWorldCoords()
        {
            return this.PositionToVector();
        }
        
        // public string ToString(string format, IFormatProvider formatProvider)
        // {
        //     if (string.IsNullOrEmpty(format))
        //         format = "F1";
        //     return
        //         $"[{(object) Lat.ToString(format, formatProvider)},{(object) Lon.ToString(format, formatProvider)}]";
        // }
        
        public override string ToString() => string.Format(NumberFormatInfo.InvariantInfo, "{0:F6},{1:F6}", Lat, Lon);

        //
        // public override string ToString()
        // {
        //     return string.Format(NumberFormatInfo.InvariantInfo, "[{0F6},{1F6}]", Lat, Lon);
        // }
    }
    
}
