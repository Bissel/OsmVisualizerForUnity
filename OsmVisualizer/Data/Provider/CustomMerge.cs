using System;
using System.Collections.Generic;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class CustomMerge : MonoBehaviour
    {

        [Serializable]
        public class IntersectionMerger
        {
            public long intersection;

            public List<long> otherIntersections = new List<long>();
        }

        public List<IntersectionMerger> customMergers = new List<IntersectionMerger>();
        
    }
}