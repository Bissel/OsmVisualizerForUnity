using System;
using System.Collections.Generic;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class CustomNodePosition : MonoBehaviour
    {
        [Serializable]
        public class NodePosition
        {
            public long node;
            public Vector2 position;
        }

        public List<NodePosition> nodePositions;
        
    }
}