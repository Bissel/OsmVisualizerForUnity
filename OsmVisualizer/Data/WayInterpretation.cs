namespace OsmVisualizer.Data
{
    public abstract class WayInterpretation
    {
        public enum Type
        {
            BUILDING,
            BUILDING_PART,
            BRIDGE,
            BRIDGE_SUPPORT,
            TUNNEL,
            LANDUSE,
            WATERWAY
        }
        
        public readonly string Id;
        public readonly Type WayType;

        protected WayInterpretation(string id, Type wayType)
        {
            Id = id;
            WayType = wayType;
        }
    }
}
