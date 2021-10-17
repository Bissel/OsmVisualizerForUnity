using System;
using System.Collections.Generic;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class CustomRemove : MonoBehaviour
    {
        [Serializable]
        public struct Lane
        {
            public long start;
            public long end;
        }
        
        public List<Lane> customRemove = new List<Lane>();
        
    }
}