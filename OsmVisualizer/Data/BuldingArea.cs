
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;

namespace OsmVisualizer.Data
{
    public class BuildingPart : WayInterpretation
    {
        public readonly BuildingCharacteristics Characteristics;
        public readonly Area Area;
        public readonly string PartType;

        public BuildingPart(string id, BuildingCharacteristics characteristics, string type, Vector2[] baseShape) : base (id, Type.BUILDING)
        {
            Characteristics = characteristics;
            PartType = type;
            
            baseShape = baseShape.ToList().Distinct().ToArray();
            
            var clockwise = baseShape.IsOrientationClockwise();
            Area = new Area( clockwise ? baseShape : baseShape.Reverse().ToArray() );
        }
    }
    public class BuildingArea : WayInterpretation
    {
        public readonly BuildingCharacteristics Characteristics;

        public readonly Area Area;

        private readonly long[] _nodes;

        public readonly List<BuildingPart> Parts;

        public BuildingArea(string id, long[] nodes, BuildingCharacteristics characteristics, Vector2[] baseShape) : base (id, Type.BUILDING)
        {
            Characteristics = characteristics;
            Parts = new List<BuildingPart>();
            
            baseShape = baseShape.ToList().Distinct().ToArray();
            
            var clockwise = baseShape.IsOrientationClockwise();
            Area = new Area( clockwise ? baseShape : baseShape.Reverse().ToArray() );
            _nodes = clockwise ? nodes : nodes.Reverse().ToArray();
        }

        public bool ContainsPart(IEnumerable<long> nodes)
        {
            return nodes.Any(n => _nodes.Contains(n));
        }
    }
}