using System.Collections.Generic;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;

namespace OsmVisualizer.Data.Characteristics
{
   
    public class WayCharacteristics
    {
        /**
         * https://wiki.openstreetmap.org/wiki/DE:Key:sidewalk
         */
        public class SidewalkCharacteristics
        {
            public readonly string Surface;
            public readonly string SurfaceFull;

            public SidewalkCharacteristics(Element element, string site)
            {
                SurfaceFull = element.GetProperty($"sidewalk:{site}:surface") ?? element.GetProperty($"sidewalk:both:surface");
                Surface = SurfaceFull?.Split(':')[0];
            }
        }
        
        /**
         * https://wiki.openstreetmap.org/wiki/DE:Key:cycleway
         */
        public class CyclewayCharacteristics
        {
            public readonly string Surface;
            public readonly string SurfaceFull;
            public readonly float Width;

            public CyclewayCharacteristics(Element element, string site)
            {
                SurfaceFull = element.GetProperty($"cycleway:{site}:surface") ?? element.GetProperty($"cycleway:both:surface");
                Surface = SurfaceFull?.Split(':')[0];
                
                Width = element.HasProperty($"cycleway:{site}:width")
                    ? element.GetPropertyFloat($"cycleway:{site}:width")
                    : element.HasProperty($"cycleway:both:width")
                        ? element.GetPropertyFloat($"cycleway:both:width")
                        : float.NaN;
            }
        }
        
        public readonly string Type;
        public readonly string Name;

        public readonly int SpeedLimit;
        public readonly string Surface;
        public readonly string SurfaceFull;
        public readonly bool IsLit;
        public readonly bool IsAccessible;
        public readonly bool IsForPublicServiceVehicles;
        public readonly bool IsForBus;

        public readonly int Layer;

        public readonly float CustomHeightOffset;


        public readonly float WidthAttr;
        public float Width { get; set; }
        
        public float WidthScaling { get; set; }
        
        public readonly SidewalkCharacteristics SidewalkLeft;
        public readonly SidewalkCharacteristics SidewalkRight;
        
        public bool HasSidewalkBoth() => SidewalkLeft != null && SidewalkRight != null;
        public bool HasSidewalk() => SidewalkLeft != null || SidewalkRight != null || SidewalkIsSeparate;
        public bool SidewalkIsSeparate { get; private set; }

        public readonly CyclewayCharacteristics CyclewayLeft;
        public readonly CyclewayCharacteristics CyclewayRight;
        public readonly bool CyclewayIsShared;

        public readonly bool IsBridge;
        public readonly bool IsTunnel;

        public readonly float MaxHeight;
        

        // public bool IsOneway { get; private set; }
        // public bool IsOnewayReverse { get; private set; }

        public WayCharacteristics(Element element, Dictionary<string, int> defaultSpeedLimits = null, int defaultSpeedLimit = 50)
        {
            Type = element.GetProperty("highway");
            Name = element.GetProperty("name");
            SurfaceFull = element.GetProperty("surface");
            IsLit = element.GetPropertyBool("lit");
            IsAccessible = !element.HasProperty("access") || element.GetPropertyBool("access");
            IsForPublicServiceVehicles = element.GetPropertyBool("psv");
            IsForBus = element.GetPropertyBool("bus");
            
            Surface = SurfaceFull?.Split(':')[0];
            SpeedLimit = element.GetPropertyInt(
                "maxspeed",
                defaultSpeedLimits?.ContainsKey(Type) ?? false
                    ? defaultSpeedLimits[Type]
                    : defaultSpeedLimit
            );
                    
                    
            //         Type switch
            // {
            //     "motorway" => 120,
            //     "motorway_link" => 120,
            //     "trunk" => 70,
            //     "trunk_link" => 70,
            //     "residential" => 30,
            //     "unclassified" => 30,
            //     "living_street" => 30,
            //     _ => 50
            // });
            
            WidthAttr = element.GetPropertyMeasurement("width");

            Layer = element.GetPropertyInt("layer", 0);

            CustomHeightOffset = element.GetPropertyFloat("heightOffset", 0.0f);

            IsBridge = element.HasProperty("bridge") && element.GetPropertyBool("bridge");
            IsTunnel = element.HasProperty("tunnel") && element.GetPropertyBool("tunnel");

            {
                SidewalkLeft = GetSidewalk(element, "left");
                SidewalkRight = GetSidewalk(element, "right");
                SidewalkIsSeparate = element.HasProperty("sidewalk") && element.GetProperty("sidewalk") == "separate";
            }

            {
                CyclewayLeft = GetCycleway(element, "left");
                CyclewayRight = GetCycleway(element, "right");
                // shared / share_busway / shared_lane
                var cycleway = element.GetProperty("cycleway");
                CyclewayIsShared = cycleway != null && cycleway.Length >= 5 && cycleway.Substring(0, 5) == "share";
            }

            // DirectionsForward = getDirection(element, true, IsOnewayReverse);
            // DirectionsBackward = getDirection(element, false, IsOnewayReverse);

            MaxHeight = element.HasProperty("maxheight") ? element.GetPropertyMeasurement("maxheight") : float.NaN;
        }

        private WayCharacteristics(string type, string name, int speedLimit, string surface, string surfaceFull,
            bool isLit, bool isAccessible, bool isForPublicServiceVehicles, bool isForBus, int layer, float widthAttr,
            bool isBridge, bool isTunnel, float maxHeight, bool cyclewayIsShared, bool sidewalkIsSeparate,
            SidewalkCharacteristics sidewalkLeft, SidewalkCharacteristics sidewalkRight,
            CyclewayCharacteristics cyclewayLeft, CyclewayCharacteristics cyclewayRight,
            float customHeightOffset
        )
        {
            Type = type;
            Name = name;
            SpeedLimit = speedLimit;
            Surface = surface;
            SurfaceFull = surfaceFull;
            IsLit = isLit;
            IsAccessible = isAccessible;
            IsForPublicServiceVehicles = isForPublicServiceVehicles;
            IsForBus = isForBus;
            
            Layer = layer;
            WidthAttr = widthAttr;

            IsBridge = isBridge;
            IsTunnel = isTunnel;

            MaxHeight = maxHeight;

            CyclewayIsShared = cyclewayIsShared;
            SidewalkIsSeparate = sidewalkIsSeparate;

            SidewalkLeft = sidewalkLeft;
            SidewalkRight = sidewalkRight;

            CyclewayLeft = cyclewayLeft;
            CyclewayRight = cyclewayRight;

            CustomHeightOffset = customHeightOffset;
        }

        public bool SameSimpleType(WayCharacteristics other)
        {
            return IsAccessible == other.IsAccessible 
                   && IsForBus == other.IsForBus
                   // @todo Type 
                ;
        }


        private static SidewalkCharacteristics GetSidewalk(Element element, string site)
        {
            var sidewalk = element.GetProperty("sidewalk");
            
            return sidewalk != null && (sidewalk == "both" || sidewalk == site)
                ? new SidewalkCharacteristics(element, site)
                : null;
        }
        
        private static CyclewayCharacteristics GetCycleway(Element element, string site)
        {
            
            var cycleway = element.GetProperty("cycleway");
            var cyclewaySite = element.GetProperty($"cycleway:{site}");
            
            return cycleway != null && (cycleway == "both" || cycleway == site) || (cyclewaySite != null && cyclewaySite != "no" )
                ? new CyclewayCharacteristics(element, site)
                : null;
        }

        public WayCharacteristics ReverseDirection()
        {
            return new WayCharacteristics(
                Type, Name, SpeedLimit, Surface, SurfaceFull, IsLit, IsAccessible, IsForPublicServiceVehicles, IsForBus,
                Layer, WidthAttr, IsBridge, IsTunnel, MaxHeight, CyclewayIsShared, SidewalkIsSeparate,
                SidewalkRight, SidewalkLeft,
                CyclewayRight, CyclewayLeft, 
                CustomHeightOffset
            );
        }

        public override bool Equals(object obj)
        {
            return obj is WayCharacteristics w 
                   && IsAccessible == w.IsAccessible 
                   && IsBridge == w.IsBridge
                   && IsTunnel == w.IsTunnel
                   && IsForBus == w.IsForBus
                   && IsForPublicServiceVehicles == w.IsForPublicServiceVehicles
                   && IsLit == w.IsLit
                   && System.Math.Abs(Width - w.Width) < .1f
                   && SpeedLimit == w.SpeedLimit
                   && Layer == w.Layer
                   && System.Math.Abs(MaxHeight - w.MaxHeight) < .1f;
        }

        public override int GetHashCode()
        {
            return (
                (
                    IsAccessible ? 1 : 0)
                   + (IsBridge ? 2 : 0)
                   + (IsTunnel ? 4 : 0)
                   + (IsForBus ? 8 : 0)
                   + (IsForPublicServiceVehicles ? 16 : 0)
                   + (IsLit ? 32 : 0
               )
               + "_" + SpeedLimit 
               + "_" + Layer
               + "_" + WidthAttr
               + "_" + MaxHeight
               + "_" + Type
               + "_" + Name
               + "_" + SurfaceFull
            ).GetHashCode();
        }
    }
}
