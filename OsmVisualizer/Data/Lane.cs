
using System.Collections.Generic;
using OsmVisualizer.Data.Types;
using UnityEngine;

namespace OsmVisualizer.Data
{

    public class Lane : Spline
    {
        
        public readonly LaneCollection LaneCollection;
        public readonly Direction[] Directions;

        public readonly List<Lane> Next = new List<Lane>();
        
        public Lane(LaneCollection laneCollection, IEnumerable<Vector2> spline, Direction[] directions) : base (spline)
        {
            LaneCollection = laneCollection;
            Directions = directions;
        }
        
        
    }

}