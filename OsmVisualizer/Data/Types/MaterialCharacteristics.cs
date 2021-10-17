namespace OsmVisualizer.Data.Types
{
    public abstract class MaterialCharacteristics
    {
        public readonly string Color;
        public readonly string Material;

        protected MaterialCharacteristics(string color, string material)
        {
            Color = color;
            Material = material == "" ? null : material;
        }
    }
}