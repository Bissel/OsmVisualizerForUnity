using OsmVisualizer.Data.Request;

namespace OsmVisualizer.Data.Characteristics
{
    /// <summary>
    /// https://wiki.openstreetmap.org/wiki/Key:railway?uselang=en
    /// </summary>
    public class RailWayCharacteristics : WayCharacteristics
    {
        /// <summary>
        /// abandoned | construction | disused | funicular | light_rail | miniature | monorail
        /// | narrow_gauge | preserved | preserved=yes | rail | subway | tram
        /// </summary>
        public readonly string RailType;
        
        /// <summary>
        /// Default Gauge in mm
        /// </summary>
        public const int DefaultGauge = 1435; 
        /// <summary>
        /// Space between the single rails in meters
        /// https://en.wikipedia.org/wiki/List_of_tram_systems_by_gauge_and_electrification
        /// 0 if RailType is monorail
        /// </summary>
        public readonly float Gauge;

        /// <summary>
        /// Usually 1
        /// https://wiki.openstreetmap.org/wiki/DE:Key:tracks
        /// </summary>
        public readonly int Tracks;
        
        /// <summary>
        /// contact_line | rail | 4th_rail | ground-level_power_supply | yes | no
        /// </summary>
        public readonly string Electrified;
        public readonly bool IsElectrified;
        
        public readonly int Voltage;
        public readonly int Frequency;

        public readonly bool EmbeddedRails;
        
        public LaneCollection LaneCollection { get; private set; }
        
        public RailWayCharacteristics(Element element) : base(element)
        { 
            RailType = element.GetProperty("railway");
            
            Gauge = (element.HasProperty("gauge") && RailType != "monorail" ? element.GetPropertyInt("gauge") : DefaultGauge) * .001f;

            EmbeddedRails = element.GetPropertyBool("embedded_rails");
            
            Tracks = element.HasProperty("tracks") ? element.GetPropertyInt("tracks") : 1;
            
            
            // https://wiki.openstreetmap.org/wiki/Key:electrified?uselang=en
            Electrified = element.GetProperty("electrified");
            if (Electrified == null || Electrified == "no") return;
            
            IsElectrified = true;
            Voltage = element.GetPropertyInt("voltage");
            Frequency = element.GetPropertyInt("frequency");
        }

        public void SetLaneCollection(LaneCollection lc)
        {
            LaneCollection = lc;
        }
    }    
    
}
