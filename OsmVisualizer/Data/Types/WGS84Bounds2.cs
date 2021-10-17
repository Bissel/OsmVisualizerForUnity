using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    public class WGS84Bounds2
    {
        public Position2 Min { get; private set; }
        public Position2 Max { get; private set; }
        
        public Position2 Center { get; private set; }

        public Vector2 Size;

        public WGS84Bounds2(Position2 center, Vector2 sizeInMeters)
        {
            CreateFromCenterWithSize(center, sizeInMeters);
        }
        
        public WGS84Bounds2(Position2 center, float sizeInMeters)
        {
            CreateFromCenterWithSize(center, new Vector2(sizeInMeters, sizeInMeters));
        }

        private void CreateFromCenterWithSize(Position2 center, Vector2 sizeInMeters)
        {
            sizeInMeters = sizeInMeters.Abs();
            
            Min = center.WithOffset(sizeInMeters * -.5f, true);
            Max = center.WithOffset(sizeInMeters * .5f, true);
            
            Center = center;
            Size = sizeInMeters;
        }
        
        public override string ToString() => $"Center: {Center}, Size: {Size}";

        public string ToMinMaxString() => $"{Min},{Max}";
    }
}
