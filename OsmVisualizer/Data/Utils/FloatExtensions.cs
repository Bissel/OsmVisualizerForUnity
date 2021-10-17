using UnityEngine;

namespace OsmVisualizer.Data.Utils
{
    
    public static class FloatExtensions
    {
        public static float Round(this float f, int precision = 100)
        {
            return float.IsNaN(f) ? f : Mathf.Round(f * precision) / precision;
        }
    }
}
