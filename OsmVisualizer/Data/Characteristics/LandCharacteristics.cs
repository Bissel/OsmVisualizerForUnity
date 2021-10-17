using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using UnityEngine;

namespace OsmVisualizer.Data.Characteristics
{
    /**
     * https://wiki.openstreetmap.org/wiki/DE:Key:landuse
     */
    public class LandCharacteristics : MaterialCharacteristics
    {

        // public readonly string Material;
        public readonly Vector2 BoundsMin;
        public readonly Vector2 BoundsMax;
        public readonly bool IsLeisure;
        
        
        public LandCharacteristics(Element element, Vector2 boundsMin, Vector2 boundsMax) 
            : base(null, element.GetProperty("landuse") ?? element.GetProperty("leisure"))
        {
            BoundsMin = boundsMin;
            BoundsMax = boundsMax;
            IsLeisure = element.HasProperty("leisure");
        }

        public bool IsCity()
        {
            switch (Material)
            {
                case "commercial": 
                case "construction": 
                case "industrial": 
                case "residential": 
                case "retail":
                    return true;
                default: return false;
            }
        }
        
        public bool IsAgriculture()
        {
            switch (Material)
            {
                case "allotments": 
                case "farmland": 
                case "farmyard": 
                case "flowerbed": 
                case "forest":
                case "meadow":
                case "orchard":
                case "vineyard":
                    return true;
                default: return false;
            }
        }
        
    }
}
