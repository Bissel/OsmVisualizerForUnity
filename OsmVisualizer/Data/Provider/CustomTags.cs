using System;
using System.Collections.Generic;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class CustomTags : MonoBehaviour
    {

        [Serializable]
        public class CustomTag
        {
            public long wayId;
            public bool use = true;
            
            public List<TagValuePair> tags = new List<TagValuePair>();

        }

        [Serializable]
        public class TagValuePair
        {
            public string tag;
            public string value;
        }

        public List<CustomTag> customTags = new List<CustomTag>();
        
    }
}