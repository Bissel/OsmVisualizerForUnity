using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Characteristics
{
    public class BuildingCharacteristics : MaterialCharacteristics 
    {
                
        public class AddressCharacteristics
        {
            public readonly string City;
            public readonly string Street;
            public readonly string No;

            public AddressCharacteristics(string city, string street, string no)
            {
                City = city;
                Street = street;
                No = no;
            }

            public override string ToString() => $"{City} {Street} {No}";
        }

        public class RoofCharacteristics : MaterialCharacteristics 
        {
            public readonly float Height;
            public readonly int Levels;
            public readonly string Shape;
            public readonly bool Lines;

            public RoofCharacteristics(string color, float height, int levels, string material, string shape, bool lines) : base(color, material)
            {
                Height = height;
                Levels = levels;
                Shape = shape;
                Lines = lines;
            }
        }

        public readonly AddressCharacteristics Address;
        public readonly RoofCharacteristics Roof;

        public readonly string Type;
        public readonly int Levels;
        public readonly int LevelMin;
        public readonly float Height;
        public readonly float HeightMin;

        public readonly bool IsFootprint;
        
        public BuildingCharacteristics(Element element) : base (
            element.GetProperty("building:colour") ?? element.GetProperty("building:color"),
            element.GetProperty("building:material")
        )
        {
            Address = GetAddress(element);
            Roof = GetRoof(element);
            
            Type = element.GetProperty("building");
            Levels = element.GetPropertyInt("building:levels");
            LevelMin = element.GetPropertyInt("building:min_level");
            Height = element.GetPropertyMeasurement("height");
            HeightMin = element.GetPropertyMeasurement("min_height");

            IsFootprint = element.HasProperty("footprint") && element.GetPropertyBool("footprint");
        }
        
        public float GetHeight(float defaultHeight = 0f) =>
            !float.IsNaN(Height) && Height > 1f
                ? Height 
                : Levels > 0
                    ? 3.5f * Levels 
                    : defaultHeight;

        public float GetHeightMin(float defaultHeight = 0f) =>
            !float.IsNaN(HeightMin) && HeightMin > 1f
                ? HeightMin 
                : LevelMin > 0
                    ? 3.5f * LevelMin 
                    : defaultHeight;

        public Color GetColor() => Color.ToColor();

        private static RoofCharacteristics GetRoof(Element element)
        {
            var color = element.GetProperty("roof:colour") ?? element.GetProperty("roof:color");
            var material = element.GetProperty("roof:material");
            var height = element.GetPropertyFloat("roof:height");
            var levels = element.GetPropertyInt("roof:levels");
            var shape = element.GetProperty("roof:shape");
            var lines = element.HasProperty("roof:lines") && element.GetPropertyBool("roof:lines");

            return color != null || material != null || !float.IsNaN(height) || levels > 0 || shape != null
                ? new RoofCharacteristics(color, height, levels, material, shape, lines)
                : null;
        }
        
        private static AddressCharacteristics GetAddress(Element element)
        {
            var city = element.GetProperty("city") ?? element.GetProperty("addr:city");
            var street = element.GetProperty("street") ?? element.GetProperty("addr:street");
            var no = element.GetProperty("housenumber") ?? element.GetProperty("addr:housenumber");

            return city != null || street != null || no != null
                ? new AddressCharacteristics(city, street, no)
                : null;
        }

        public override string ToString()
        {
            return
                $"H: {HeightMin}+{Height}, Address: {Address}, Color: {Color}, IsFootprint: {(IsFootprint ? "yes" : "no")}";
        }
    }
}