
using System.Linq;

namespace OsmVisualizer.Data.Request
{
    [System.Serializable]
    public class Result
    {
        [System.Serializable]
        public class OSM_3s
        {
            public string timestamp_osm_base;
            public string copyright;
        }
    
        public string version;
        public string generator;
        public OSM_3s osm3s;
        public Element[] elements;

        public override string ToString()
        {
            return "{"
                   + "version: " + version + "\n"
                   + "generator: " + generator + "\n"
                   + "elementsCount: " + elements.Length + "\n"
                   + "elements: [" + "\n"
                   + elements.Aggregate("", (current, e) => current + "  " + e + ", \n")
                   + "]" + "\n"
                   + "}";
        }
    }
    
}
