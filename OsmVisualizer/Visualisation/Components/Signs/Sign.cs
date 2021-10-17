namespace OsmVisualizer.Visualisation.Components.Signs
{
    public class Sign
    {
        public enum SignType
        {
            SpeedLimit, SpeedLimitEnd, Stop, GiveWay, PrioritySingle, PriorityRoad, PriorityRoadEnd, OneWay, Rail
        }

        public readonly SignType Type;
        public readonly string Data;

        public Sign(SignType type, string data = null)
        {
            Type = type;
            Data = data;
        }
        
        public Sign(SignType type, int data)
        {
            Type = type;
            Data = "" + data;
        }
    }
}