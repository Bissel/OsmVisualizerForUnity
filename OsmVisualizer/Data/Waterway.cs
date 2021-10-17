using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Types;
using UnityEngine;

namespace OsmVisualizer.Data
{
    public class NaturalWater : WayInterpretation
    {
        public readonly Area Area;

        public NaturalWater(string id, List<Vector2> baseShape) : base(id, Type.WATERWAY)
        {
            Area = new Area(baseShape);
        }
    }
    public class Coastline : WayInterpretation
    {
        public readonly Area Area;

        public Coastline(string id, List<Vector2> spline) : base(id, Type.WATERWAY)
        {
            Area = new Area(spline);
        }
    }
    
    public class Waterway : WayInterpretation
    {
        public readonly string WaterwayType;
        public readonly string Name;
        public readonly float Width;

        public readonly Area Flow;
        private readonly long[] _nodes;

        public readonly List<NaturalWater> Areas = new List<NaturalWater>();
        public readonly List<Coastline> Coastlines = new List<Coastline>();
        
        public Waterway(string id, string type, string name, float width, List<Vector2> spline, long[] nodes) : base(id, WayInterpretation.Type.WATERWAY)
        {
            WaterwayType = type;
            Width = width;
            Name = name;

            Flow = new Area(spline);
            _nodes = nodes.Where(n => n != 0).ToArray();
        }
        
        public bool ContainsWaterArea(IEnumerable<long> nodes)
        {
            return nodes.Any(n => _nodes.Contains(n));
        }
        
        public bool ContainsCoastline(IEnumerable<long> nodes)
        {
            return nodes.Any(n => _nodes.Contains(n));
        }
    }
}
