using System.Collections.Generic;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using UnityEngine;

namespace OsmVisualizer.Data
{
    public class Landuse : WayInterpretation
    {
        public readonly LandCharacteristics Characteristics;
        public readonly Area Area;

        public Landuse(string id, LandCharacteristics characteristics, List<Vector2> baseShape) : base(id, Type.LANDUSE)
        {
            Characteristics = characteristics;
            Area = new Area(baseShape);
        }
    }
}
