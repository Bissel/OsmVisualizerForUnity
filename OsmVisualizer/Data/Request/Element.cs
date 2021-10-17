
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;

namespace OsmVisualizer.Data.Request
{
    public enum GeometryType
    {
        POINT,
        LINE,
        AREA
    }

    public class ElementBounds
    {
        public float minlat;
        public float minlon;
        public float maxlat;
        public float maxlon;

        public Position2 GetMin() => new Position2(minlat, minlon);
        public Position2 GetMax() => new Position2(maxlat, maxlon);
    }
    
    [System.Serializable]
    public class Element
    {
        public string type;
        public string id;
        public long[] nodes; // IDs
        public ElementBounds bounds;
        public Position2[] geometry; // Node positions
        public GeometryType GeometryType = GeometryType.POINT;
        public Dictionary<string, string> tags = new Dictionary<string, string>();

        public bool used = false;
        public bool insideTile = false;
        public bool split = false;
        public List<MapData.LaneId> SplitElements = null;

        public List<Vector2> pointsV2 = null;
        public List<bool> pointsOutOfBounds = null;

        public int pointsV2Offset { get; set; }

        public override string ToString()
        {
            return string.Format(
                NumberFormatInfo.InvariantInfo, 
                "Request.Element::[id:{0}, type:{1}, tags: {2}, geometry: {3}, GeometryType: {4}]",
                id,
                type,
                tags.ToString(),
                geometry,
                GeometryType
            );
        }
        
        public bool GetPropertyBool(string key)
        {
            var property = GetProperty(key);
            return property != null && (property.ToLower() == "true" || property.ToLower() == "yes" || property.ToLower() == "1");
        }

        public int GetPropertyInt(string key, int defaultVal = 0)
        {
            var property = GetProperty(key);
            return property != null && int.TryParse(property, out var propInt) ? propInt : defaultVal;
        }

        public float GetPropertyFloat(string key, float defaultVal = .0f)
        {
            var property = GetProperty(key);
            return property != null && TryParseFloat(property, out var propFloat) ? propFloat : defaultVal;
        }

        public bool HasProperty(string key)
        {
            return tags.ContainsKey(key);
        }

        public string GetProperty(string key)
        {
            return tags.ContainsKey(key)
                ? tags[key]
                : null;
        }

        public float GetPropertyMeasurement(string key, float defaultValue = float.NaN)
        {
            var measurement = GetProperty(key);

            if (measurement == null)
                return defaultValue;
            
            var ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            
            float tmp;
            if (measurement.Contains("'") || measurement.Contains("\""))
            {
                var feetPos = measurement.IndexOf("'", StringComparison.Ordinal);
                var inchPos = measurement.IndexOf("\"", StringComparison.Ordinal);

                var inchStart = feetPos + 1;
                var inchLength = inchPos - inchStart;
                
                var feet = measurement.Substring(0, feetPos);
                var inch = measurement.Substring(inchStart, inchLength);
                
                var feetInM = feetPos >= 0 && TryParseFloat(feet, out tmp)
                    ? tmp * Math.Math.feet_in_m
                    : 0f;

                var inchInM = inchPos >= 0 && TryParseFloat(inch, out tmp)
                    ? tmp * Math.Math.inch_in_m
                    : 0f;
                
                // Debug.Log("measurement");
                // Debug.Log(measurement);
                // Debug.Log(feet);
                // Debug.Log(inch);
                //
                // Debug.Log($"{feetInM} {inchInM} {(feetInM + inchInM)}");
                
                return feetInM + inchInM;
            }

            return measurement.Contains(" ") 
                ? (TryParseFloat(measurement.Substring(0, measurement.IndexOf(" ", StringComparison.Ordinal)), out tmp)
                    ? tmp * (
                        measurement.Contains(" mi") 
                            ? Math.Math.mile_in_m 
                            : measurement.Contains(" km") 
                                ? 0.001f
                                : 1f
                    )
                    : float.NaN)
                : (TryParseFloat(measurement.OnlyNumberChars(), out tmp)
                    ? tmp
                    : float.NaN);
        }

        private static bool TryParseFloat(string s, out float result)
        {
            return float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        public override bool Equals(object obj)
        {
            return obj is Element element 
                   && element.nodes.Length == nodes.Length
                   && element.type == type
                   && element.GeometryType == GeometryType
                   && element.nodes[0] == nodes[0]
                   && element.nodes[nodes.Length - 1] == nodes[nodes.Length - 1];
        }

        public override int GetHashCode()
        {
            var hashCode = nodes.Length.GetHashCode();
            hashCode = (hashCode * 397) ^ type.GetHashCode();
            hashCode = (hashCode * 397) ^ GeometryType.GetHashCode();
            hashCode = (hashCode * 397) ^ nodes[0].GetHashCode();
            hashCode = (hashCode * 397) ^ nodes[nodes.Length - 1].GetHashCode();
            return hashCode;
        }

        public Element ElementFromRange(int startIndex, int endIndex, Vector2 tileMin, Vector2 tileMax)
        {
            if (startIndex == 0 && endIndex == pointsV2.Count - 1)
                return this;
            
            var skip = startIndex;
            var take = endIndex - startIndex + 1;

            var newPoints = pointsV2.Skip(skip).Take(take).ToList();
            var newOob = pointsOutOfBounds.Skip(skip).Take(take).ToList();
            
            return new Element
            {
                type = type,
                id = $"{id}[{startIndex}/{endIndex}]",
                bounds = bounds,
                nodes = nodes.Skip(skip).Take(take).ToArray(),
                geometry = geometry?.Skip(skip).Take(take).ToArray(),
                pointsV2 = newPoints,
                pointsOutOfBounds = newOob,
                GeometryType = GeometryType,
                tags = tags,
                insideTile = newOob.All(p => !p)
            };
        }
    }
}

