using System.Collections;
using System.Collections.Generic;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Request;

namespace OsmVisualizer.Data.Provider
{
    public class ConvertToLanduse : Provider
    {

        public const string ElementType = "way";
        
        public const string KeyLanduse = "landuse";
        public const string KeyLeisure = "leisure";
        
        public ConvertToLanduse(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Landuse) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach(var element in request.elements) 
            {
                if (!element.insideTile || element.used || element.GeometryType != GeometryType.AREA 
                    || element.type != ElementType)
                    continue;
                
                if(! (element.HasProperty(KeyLanduse) || element.HasProperty(KeyLeisure)) )
                    continue;
                
                ConvertElementToLanduse(element, tile.WayAreas);
                element.used = true;

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            stopwatch.Stop();
        }

        private static void ConvertElementToLanduse(Element element, Dictionary<string, WayInterpretation> data)
        {
            data.Add( 
                element.id, 
                new Landuse(
                    element.id, 
                    new LandCharacteristics(element, element.bounds.GetMin().InWorldCoords(), element.bounds.GetMax().InWorldCoords()), 
                    element.pointsV2
                )
            );
        }
        
    }
}
